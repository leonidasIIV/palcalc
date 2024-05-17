﻿using PalCalc.Model;
using PalCalc.Solver.PalReference;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PalCalc.Solver.ResultPruning
{
    // choose results where the inputs are in preferred locations
    public class PreferredLocationPruning : IResultPruning
    {
        public PreferredLocationPruning(CancellationToken token) : base(token)
        {
        }

        // prefer pals in palbox, then in base, etc
        public static int LocationOrderingOf(LocationType type) => type switch
        {
            LocationType.Palbox => 0,
            LocationType.Base => 100,
            LocationType.PlayerParty => 10000,
            _ => throw new NotImplementedException()
        };

        public override IEnumerable<IPalReference> Apply(IEnumerable<IPalReference> results) =>
            FirstGroupOf(results, r =>
            {
                var countsByLocationType = new Dictionary<LocationType, int>
                {
                    { LocationType.Palbox, 0 },
                    { LocationType.Base, 0 },
                    { LocationType.PlayerParty, 0 },
                };

                foreach (var pref in r.AllReferences())
                {
                    switch (pref.Location)
                    {
                        case OwnedRefLocation orl:
                            countsByLocationType[orl.Location.Type] += 1; break;

                        case CompositeRefLocation crl:
                            var maleLoc = crl.MaleLoc as OwnedRefLocation;
                            var femaleLoc = crl.FemaleLoc as OwnedRefLocation;

                            countsByLocationType[maleLoc.Location.Type] += 1;
                            if (maleLoc.Location.Type != femaleLoc.Location.Type)
                                countsByLocationType[femaleLoc.Location.Type] += 1;

                            break;
                    }
                }

                return
                    countsByLocationType[LocationType.Palbox] * LocationOrderingOf(LocationType.Palbox) +
                    countsByLocationType[LocationType.Base] * LocationOrderingOf(LocationType.Base) +
                    countsByLocationType[LocationType.PlayerParty] * LocationOrderingOf(LocationType.PlayerParty)
                ;
            });
    }
}
