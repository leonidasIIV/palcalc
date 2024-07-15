﻿using PalCalc.Model;
using PalCalc.UI.Localization;
using PalCalc.UI.ViewModel.Mapped;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PalCalc.UI.ViewModel
{
    public class TraitCollectionViewModel
    {
        // for XAML designer view
        public TraitCollectionViewModel() : this(
            new List<TraitViewModel>()
            {
                new TraitViewModel(),
                new TraitViewModel(),
                new TraitViewModel(),
            })
        {
        }

        public TraitCollectionViewModel(IEnumerable<TraitViewModel> traits)
        {
            Traits = traits.ToList();

            RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            for (int i = 1; i < NumRows; i++)
            {
                if (i % 2 == 1)
                {
                    RowDefinitions.Add(new RowDefinition() { Height = new GridLength(Spacing) });
                }
                else
                {
                    RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
                }
            }

            ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(Spacing) });
            ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

            if (!Traits.Any())
            {
                Description = Translator.Translations[LocalizationCodes.LC_TRAITS_COUNT_EMPTY].Bind();
            }
            else
            {
                var definite = traits.Where(t => t.ModelObject is not IUnknownTrait);
                var random = traits.Where(t => t.ModelObject is RandomTrait);
                var unrecognized = traits.Where(t => t.ModelObject is UnrecognizedTrait);

                var parts = new List<ILocalizedText>(definite.Select(t => t.Name));

                if (random.Any())
                    parts.Add(
                        Translator.Translations[LocalizationCodes.LC_TRAITS_COUNT_RANDOM].Bind(new() { { "NumRandom", random.Count() } })
                    );

                if (unrecognized.Any())
                    parts.Add(
                        Translator.Translations[LocalizationCodes.LC_TRAITS_COUNT_UNRECOGNIZED].Bind(new() { { "NumUnrecognized", unrecognized.Count() } })
                    );

                Description = Translator.Join.Bind(parts);
            }

            RequiredDescription = Translator.Translations[LocalizationCodes.LC_REQUIRED_TRAITS_SUMMARY].Bind(
                new()
                {
                    { "TraitsList", Description }
                }
            );

            OptionalDescription = Translator.Translations[LocalizationCodes.LC_OPTIONAL_TRAITS_SUMMARY].Bind(
                new()
                {
                    { "TraitsList", Description }
                }
            );
        }

        public ILocalizedText Description { get; }

        public ILocalizedText RequiredDescription { get; }
        public ILocalizedText OptionalDescription { get; }

        public List<TraitViewModel> Traits { get; }

        public int Spacing => 3;

        public int EntriesPerRow => 2;

        public int NumRows
        {
            get
            {
                if (Traits.Count <= 2) return 1;
                else return 3;
            }
        }

        public List<RowDefinition> RowDefinitions { get; } = new List<RowDefinition>();
        public List<ColumnDefinition> ColumnDefinitions { get; } = new List<ColumnDefinition>();

        public int RowIndexOf(TraitViewModel trait)
        {
            var mainRow = Traits.IndexOf(trait) / EntriesPerRow;
            if (mainRow == 0) return mainRow;
            else return mainRow + 1;
        }

        public int ColumnIndexOf(TraitViewModel trait)
        {
            var mainColumn = Traits.IndexOf(trait) % EntriesPerRow;
            if (mainColumn == 0) return mainColumn;
            else return mainColumn + 1;
        }
    }
}
