﻿using CommunityToolkit.Mvvm.ComponentModel;
using PalCalc.Model;
using PalCalc.SaveReader;
using PalCalc.Solver;
using PalCalc.UI.Model;
using PalCalc.UI.View;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PalCalc.UI.ViewModel
{
    internal partial class MainWindowViewModel : ObservableObject
    {
        private static PalDB db = PalDB.LoadEmbedded();

        // main app model
        public MainWindowViewModel()
        {
            SaveSelection = new SaveSelectorViewModel(SavesLocation.AllLocal);
            SolverControls = new SolverControlsViewModel();
            PalTargetList = new PalTargetListViewModel();
            PalTarget = new PalTargetViewModel();
            BreedingResults = new BreedingResultListViewModel();
        }

        public void RunSolver()
        {
            var currentSpec = PalTarget.CurrentPalSpecifier.ModelObject;
            if (currentSpec == null) return;

            var solver = SolverControls.ConfiguredSolver(SaveSelection.SelectedGame.CachedValue.OwnedPals);
            var results = solver.SolveFor(currentSpec);

            BreedingResults.Results = results.Select(r => new BreedingResultViewModel(r)).ToList();
        }

        public SaveSelectorViewModel SaveSelection { get; private set; }
        public SolverControlsViewModel SolverControls { get; private set; }
        public PalTargetListViewModel PalTargetList { get; private set; }
        public PalTargetViewModel PalTarget { get; private set; }
        public BreedingResultListViewModel BreedingResults { get; private set; }

        public PalDB DB => db;
    }
}
