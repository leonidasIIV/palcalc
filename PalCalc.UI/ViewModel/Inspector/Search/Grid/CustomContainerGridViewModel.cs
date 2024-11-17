﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PalCalc.Model;
using PalCalc.UI.Localization;
using PalCalc.UI.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace PalCalc.UI.ViewModel.Inspector.Search.Grid
{
    public partial class ContainerGridCustomPalSlotViewModel(CustomPalInstanceViewModel instance)
        : ObservableObject, IContainerGridInspectableSlotViewModel
    {
        public CustomPalInstanceViewModel PalInstance => instance;

        [ObservableProperty]
        private bool matches = true;
    }

    public class ContainerGridNewPalSlotViewModel : IContainerGridSlotViewModel
    {
        public bool Matches => false;
    }

    public partial class CustomContainerGridViewModel : ObservableObject, IContainerGridViewModel
    {
        private CustomContainerViewModel container;
        public CustomContainerGridViewModel(CustomContainerViewModel container)
        {
            this.container = container;
            Slots = new ObservableCollection<IContainerGridSlotViewModel>(
                container.Contents.Select(vm => new ContainerGridCustomPalSlotViewModel(vm))
            );

            Slots.Add(new ContainerGridNewPalSlotViewModel());

            container.Contents.CollectionChanged += Contents_CollectionChanged;

            DeleteCommand = new RelayCommand<IContainerGridSlotViewModel>(
                item =>
                {
                    if (item is ContainerGridCustomPalSlotViewModel)
                    {
                        var slot = (ContainerGridCustomPalSlotViewModel)item;
                        container.Contents.Remove(slot.PalInstance);
                    }
                }
            );
        }

        private void Contents_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems.Count != 1) throw new NotImplementedException();

                    var newItem = (CustomPalInstanceViewModel)e.NewItems[0];
                    var newSlot = new ContainerGridCustomPalSlotViewModel(newItem);
                    Slots.Insert(e.NewStartingIndex, newSlot);

                    if (SelectedSlot is ContainerGridNewPalSlotViewModel)
                    {
                        SelectedSlot = newSlot;
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems.Count != 1) throw new NotImplementedException();

                    var removedItem = e.OldItems[0];
                    if ((Slots[e.OldStartingIndex] as ContainerGridCustomPalSlotViewModel).PalInstance != removedItem)
                        throw new NotImplementedException();

                    Slots.RemoveAt(e.OldStartingIndex);
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        [ObservableProperty]
        private int perRow; 

        public ISearchCriteria SearchCriteria
        {
            set
            {
                foreach (var slot in Slots.OfType<ContainerGridCustomPalSlotViewModel>())
                    slot.Matches = slot.PalInstance.IsValid && value.Matches(slot.PalInstance.ModelObject);

                if (SelectedSlot != null && !SelectedSlot.Matches)
                    SelectedSlot = null;
            }
        }

        public Visibility GridVisibility => Visibility.Visible;

        public ILocalizedText Title => null;

        public Visibility TitleVisibility => Visibility.Collapsed;

        private IContainerGridSlotViewModel selectedSlot;
        public IContainerGridSlotViewModel SelectedSlot
        {
            get => selectedSlot;
            set
            {
                if (SetProperty(ref selectedSlot, value))
                {
                    if (value is ContainerGridNewPalSlotViewModel)
                    {
                        container.Contents.Add(
                            new CustomPalInstanceViewModel(container.Label)
                        );
                    }
                }
            }
        }

        public ObservableCollection<IContainerGridSlotViewModel> Slots { get; }

        // TODO
        public IRelayCommand<IContainerGridSlotViewModel> DeleteCommand { get; }
    }
}