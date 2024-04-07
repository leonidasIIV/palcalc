﻿using PalCalc.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PalCalc.Solver
{
    public class Solver
    {
        // returns number of ways you can choose k combinations from a list of n
        // TODO - is this the right way to use pascal's triangle??
        static int Choose(int n, int k) => PascalsTriangle.Instance[n - 1][k - 1];

        PalDB db;
        List<PalInstance> ownedPals;

        int maxBreedingSteps, maxWildPals, maxIrrelevantTraits;
        TimeSpan maxEffort;

        /**
         * TODO - how to make this show with intellisense?
         * 
         * Parameters:
         *  maxBreedingSteps: 
         *  
         *  maxWildPals: 
         *  
         *  maxIrrelevantTraits:
         *    max num. irrelevant traits from any parents or children involved in the final breeding steps (including target pal)
         *    (lower value runs faster, but considers fewer possibilities)
         *    
         *  maxEffort:
         *    effort in estimated time to get the desired pal w/ traits
         *    - goes by constant breeding time
         *    - ignores hatching time
         *    - roughly estimates time to catch wild pals with increasing time based on paldex number
         */
        public Solver(PalDB db, List<PalInstance> ownedPals, int maxBreedingSteps, int maxWildPals, int maxIrrelevantTraits, TimeSpan maxEffort)
        {
            this.db = db;
            this.ownedPals = ownedPals;
            this.maxBreedingSteps = maxBreedingSteps;
            this.maxWildPals = maxWildPals;
            this.maxIrrelevantTraits = Math.Min(3, maxIrrelevantTraits);
            this.maxEffort = maxEffort;
        }

        int NumWildPalParticipants(IPalReference pref)
        {
            switch (pref)
            {
                case BredPalReference bpr: return NumWildPalParticipants(bpr.Parent1) + NumWildPalParticipants(bpr.Parent2);
                case OwnedPalReference opr: return 0;
                case WildcardPalReference wpr: return 1;
                default: throw new Exception($"Unhandled pal reference type {pref.GetType()}");
            }
        }

        int NumBredPalParticipants(IPalReference pref)
        {
            switch (pref)
            {
                case BredPalReference bpr: return 1 + NumBredPalParticipants(bpr.Parent1) + NumBredPalParticipants(bpr.Parent2);
                default: return 0;
            }
        }

        public List<IPalReference> SolveFor(PalInstance targetInstance)
        {
            if (targetInstance.Traits.Count > GameConfig.MaxTotalTraits)
            {
                throw new Exception("Target trait count cannot exceed max number of traits for a single pal");
            }

            var relevantPals = PalCalcUtils
               .RelevantInstancesForTraits(db, ownedPals, targetInstance.Traits)
               .Where(p => p.Traits.Except(targetInstance.Traits).Count() <= maxIrrelevantTraits)
               .ToList();

            Console.WriteLine(
                "Using {0}/{1} pals as relevant inputs with traits:\n- {2}",
                relevantPals.Count,
                ownedPals.Count,
                string.Join("\n- ",
                    relevantPals
                        .OrderBy(p => p.Pal.Name)
                        .ThenBy(p => p.Gender)
                        .ThenBy(p => string.Join(" ", p.Traits.OrderBy(t => t.Name)))
                )
            );

            // `relevantPals` is now a list of all captured Pal types, where multiple of the same pal
            // may be included if they have different genders and/or different matching subsets of
            // the desired traits

            bool WithinBreedingSteps(Pal pal, int maxSteps) => db.MinBreedingSteps[pal][targetInstance.Pal] <= maxSteps;

            var workingSet = new WorkingSet(relevantPals.Where(pi => WithinBreedingSteps(pi.Pal, maxBreedingSteps)).Select(i => new OwnedPalReference(i)));
            if (maxWildPals > 0)
            {
                workingSet.AddFrom(
                    db.Pals
                        .Where(p => !relevantPals.Any(i => i.Pal == p))
                        .Where(p => WithinBreedingSteps(p, maxBreedingSteps))
                        .SelectMany(p => Enumerable.Range(0, maxIrrelevantTraits).Select(numTraits => new WildcardPalReference(p, numTraits)))
                        .Where(pi => pi.BreedingEffort <= maxEffort)
                );
            }

            Console.WriteLine("Using {0} pals for graph search:\n- {1}", workingSet.Content.Count, string.Join("\n- ", workingSet.Content));

            for (int s = 0; s < maxBreedingSteps; s++)
            {
                Console.WriteLine($"Starting search step #{s + 1} with {workingSet.Content.Count} relevant pals");
                var newInstances = Enumerable.Zip(workingSet.Content, Enumerable.Range(0, workingSet.Content.Count))
                    .AsParallel()
                    .SelectMany(pair =>
                    {
                        var parent1 = pair.First;
                        var idx = pair.Second;

                        var res = workingSet.Content
                            .Skip(idx + 1) // only search (p1,p2) pairs, not (p1,p2) and (p2,p1)
                            .Where(i => i.IsCompatibleGender(parent1.Gender))
                            .Where(i => i != null)
                            .Where(parent2 => NumWildPalParticipants(parent1) + NumWildPalParticipants(parent2) <= maxWildPals)
                            .Where(parent2 =>
                            {
                                var childPal = db.BreedingByParent[parent1.Pal][parent2.Pal].Child;
                                return db.MinBreedingSteps[childPal][targetInstance.Pal] <= maxBreedingSteps - s - 1;
                            })
                            .Where(parent2 => NumBredPalParticipants(parent1) + NumBredPalParticipants(parent2) < maxBreedingSteps)
                            .Where(parent2 =>
                            {
                                // if we disallow any irrelevant traits, neither parents have a useful trait, and at least 1 parent
                                // has an irrelevant trait, then it's impossible to breed a child with zero total traits
                                //
                                // (child would need to have zero since there's nothing useful to inherit and we disallow irrelevant traits,
                                //  impossible to have zero since a child always inherits at least 1 direct trait if possible)
                                if (maxIrrelevantTraits > 0) return true;

                                var combinedTraits = parent1.Traits.Concat(parent2.Traits);


                                var anyRelevantFromParents = targetInstance.Traits.Intersect(combinedTraits).Any();
                                var anyIrrelevantFromParents = combinedTraits.Except(targetInstance.Traits).Any();

                                return anyRelevantFromParents || !anyIrrelevantFromParents;

                            })
                            .SelectMany(parent2 =>
                            {
                                // we have two parents but don't necessarily have definite genders for them, figure out which parent should have which
                                // gender (if they're wild/bred pals) for the least overall effort (different pals have different gender probabilities)
                                List<IPalReference> ParentOptions(IPalReference parent) => parent.Gender == PalGender.WILDCARD
                                    ? new List<IPalReference>() { parent.WithGuaranteedGender(db, PalGender.MALE), parent.WithGuaranteedGender(db, PalGender.FEMALE) }
                                    : new List<IPalReference>() { parent };

                                (IPalReference, IPalReference) PreferredParentsGenders()
                                {
                                    var optionsParent1 = ParentOptions(parent1);
                                    var optionsParent2 = ParentOptions(parent2);

                                    var parentPairOptions = optionsParent1.SelectMany(p1v => optionsParent2.Where(p2v => p2v.IsCompatibleGender(p1v.Gender)).Select(p2v => (p1v, p2v))).ToList();
                                    var optimalTime = parentPairOptions.Min(pair => pair.p1v.BreedingEffort + pair.p2v.BreedingEffort);

                                    parentPairOptions = parentPairOptions.Where(pair => pair.p1v.BreedingEffort + pair.p2v.BreedingEffort == optimalTime).ToList();
                                    if (parentPairOptions.Select(pair => pair.p1v.BreedingEffort + pair.p2v.BreedingEffort).Distinct().Count() == 1)
                                    {
                                        // either there is no preference or at least 1 parent already has a specific gender
                                        if (parent2.Gender == PalGender.WILDCARD) return (parent1, parent2.WithGuaranteedGender(db, parent1.Gender.OppositeGender()));
                                        if (parent1.Gender == PalGender.WILDCARD) return (parent1.WithGuaranteedGender(db, parent2.Gender.OppositeGender()), parent2);

                                        // neither parents are wildcards
                                        return (parent1, parent2);
                                    }
                                    else
                                    {
                                        return parentPairOptions.OrderBy(p => p.p1v.BreedingEffort + p.p2v.BreedingEffort).First();
                                    }
                                }

                                var (preferredParent1, preferredParent2) = PreferredParentsGenders();

                                var parentTraits = parent1.Traits.Concat(parent2.Traits).Distinct().ToList();
                                var desiredParentTraits = targetInstance.Traits.Intersect(parentTraits).ToList();

                                var possibleResults = new List<IPalReference>();

                                var probabilityForUpToNumTraits = 0.0f;

                                // go through each potential final number of traits, accumulate the probability of any of these exact options
                                // leading to the desired traits within MAX_IRRELEVANT_TRAITS
                                for (int numFinalTraits = 0; numFinalTraits <= GameConfig.MaxTotalTraits; numFinalTraits++)
                                {
                                    // only looking for probability of getting all desired parent traits, which means we need at least Count(desired)
                                    // total traits
                                    if (numFinalTraits < desiredParentTraits.Count) continue;

                                    // exceeding Count(desiredTraits) + MAX_IRRELEVANT_TRAITS means we've exceeded the max irrelevant traits allowed
                                    if (numFinalTraits > desiredParentTraits.Count + maxIrrelevantTraits) break;

                                    float initialProbability = probabilityForUpToNumTraits;

                                    for (int numInheritedFromParent = desiredParentTraits.Count; numInheritedFromParent <= numFinalTraits; numInheritedFromParent++)
                                    {
                                        // we may inherit more traits from the parents than the parents actually have (e.g. inherit 4 traits from parents with
                                        // 2 total traits), in which case we'd still inherit just two
                                        //
                                        // this doesn't affect probabilities of getting `numInherited`, but it affects the number of random traits which must
                                        // be added to each `numFinalTraits` and the number of combinations of parent traits that we check
                                        var actualNumInheritedFromParent = Math.Min(numInheritedFromParent, parentTraits.Count);

                                        var numIrrelevantFromParent = actualNumInheritedFromParent - desiredParentTraits.Count;
                                        var numIrrelevantFromRandom = numFinalTraits - (numIrrelevantFromParent + desiredParentTraits.Count);

                                        // can inherit at most 3 random traits; if this `if` is `true` then we've hit a case which would never actually happen
                                        // (e.g. 4 target final traits, 0 from parents, 4 from random)
                                        if (numIrrelevantFromRandom > 3) continue;

#if DEBUG
                                        if (numIrrelevantFromRandom < 0) Debugger.Break();
#endif

                                        float probabilityGotRequiredFromParent;
                                        if (numInheritedFromParent == 0)
                                        {
                                            // would only happen if neither parent has a desired trait

                                            // the only way we could get zero inherited traits is if neither parent actually has any traits, otherwise
                                            // it (seems to) be impossible to get zero direct inherited traits (unconfirmed from reddit thread)
                                            if (parentTraits.Count > 0) continue;

                                            // if neither parent has any traits, we'll always get 0 inherited traits, so we'll always get the "required"
                                            // traits regardless of the roll for `TraitProbabilityDirect`
                                            probabilityGotRequiredFromParent = 1.0f;
                                        }
                                        else if (!desiredParentTraits.Any())
                                        {
                                            // just the chance of getting this number of traits from parents
                                            probabilityGotRequiredFromParent = GameConfig.TraitProbabilityDirect[numInheritedFromParent];
                                        }
                                        else if (numIrrelevantFromParent == 0)
                                        {
                                            // chance of getting exactly the required traits
                                            probabilityGotRequiredFromParent = GameConfig.TraitProbabilityDirect[numInheritedFromParent] / Choose(parentTraits.Count, desiredParentTraits.Count);
                                        }
                                        else
                                        {
                                            // (available traits except desired)
                                            // choose
                                            // (required num irrelevant)
                                            var numCombinationsWithIrrelevantTrait = (float)Choose(parentTraits.Count - desiredParentTraits.Count, numIrrelevantFromParent);

                                            // (all available traits)
                                            // choose
                                            // (actual num inherited from parent)
                                            var numCombinationsWithAnyTraits = (float)Choose(parentTraits.Count, actualNumInheritedFromParent);

                                            // probability of those traits containing the desired traits
                                            // (doesn't affect anything if we don't actually want any of these traits)
                                            // (TODO - is this right? got this simple division from chatgpt)
                                            var probabilityCombinationWithDesiredTraits = desiredParentTraits.Count == 0 ? 1 : (
                                                numCombinationsWithIrrelevantTrait / numCombinationsWithAnyTraits
                                            );

                                            probabilityGotRequiredFromParent = probabilityCombinationWithDesiredTraits * GameConfig.TraitProbabilityDirect[numInheritedFromParent];
                                        }

#if DEBUG
                                        if (probabilityGotRequiredFromParent > 1) Debugger.Break();
#endif

                                        var probabilityGotExactRequiredRandom = GameConfig.TraitRandomAddedProbability[numIrrelevantFromRandom];
                                        probabilityForUpToNumTraits += probabilityGotRequiredFromParent * probabilityGotExactRequiredRandom;
                                    }

                                    if (probabilityForUpToNumTraits <= 0) continue;

#if DEBUG
                                    if (initialProbability == probabilityForUpToNumTraits) Debugger.Break();
#endif

                                    // (not entirely correct, since some irrelevant traits may be specific and inherited by parents. if we know a child
                                    //  may have some specific trait, it may be efficient to breed that child with another parent which also has that
                                    //  irrelevant trait, which would increase the overall likelyhood of a desired trait being inherited)
                                    var potentialIrrelevantTraits = Enumerable
                                        .Range(0, Math.Max(0, numFinalTraits - desiredParentTraits.Count))
                                        .Select(i => new RandomTrait());

                                    possibleResults.Add(new BredPalReference(
                                        db.BreedingByParent[parent1.Pal][parent2.Pal].Child,
                                        preferredParent1,
                                        preferredParent2,
                                        desiredParentTraits.Concat(potentialIrrelevantTraits).ToList(),
                                        probabilityForUpToNumTraits
                                    ));
                                }

                                return possibleResults;
                            })
                            .Where(result => result.BreedingEffort <= maxEffort)
                            .ToList();

                        return res;
                    })
                    .ToList();

                Console.WriteLine("Filtering {0} potential new instances", newInstances.Count);

                var numChanged = workingSet.AddFrom(newInstances);

                if (numChanged == 0)
                {
                    Console.WriteLine("Last pass found no new useful options, stopping iteration early");
                    break;
                }
            }

            return workingSet.Content.Where(pref => pref.Pal == targetInstance.Pal && !targetInstance.Traits.Except(pref.Traits).Any()).ToList();
        }
    }
}
