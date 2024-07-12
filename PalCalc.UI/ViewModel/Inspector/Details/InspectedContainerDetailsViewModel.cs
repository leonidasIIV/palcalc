﻿using CommunityToolkit.Mvvm.ComponentModel;
using PalCalc.Model;
using PalCalc.SaveReader.SaveFile.Support.Level;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PalCalc.UI.ViewModel.Inspector.Details
{
    public partial class InspectedContainerDetailsViewModel(
        OwnerViewModel owner,
        List<PalInstance> containedPals,
        List<GvasCharacterInstance> containedRawPals,
        PalContainer rawContainer,
        LocationType? locationType
    ) : ObservableObject
    {
        // TODO - change from SingleOrDefault to Single when human pals are handled
        public List<IContainerSlotDetailsViewModel> Slots { get; } = Enumerable.Range(0, rawContainer.MaxEntries)
            .Select(i => rawContainer.Slots.SingleOrDefault(s => s.SlotIndex == i))
            .Select<PalContainerSlot, IContainerSlotDetailsViewModel>(s =>
            {
                if (s == null || s.InstanceId == Guid.Empty) return new EmptyPalContainerSlotDetailsViewModel();

                var rawChar = containedRawPals.SingleOrDefault(p => p.InstanceId == s.InstanceId);
                var rawPal = containedPals.SingleOrDefault(p => p.InstanceId == s.InstanceId.ToString());

                return new PalContainerSlotDetailsViewModel(s.InstanceId.ToString(), rawPal, rawChar);
            })
            .ToList();

        public int TotalSlots => rawContainer.MaxEntries;
        public int UsedSlots => rawContainer.NumEntries;

        public string Id => rawContainer.Id;
        public OwnerViewModel Owner { get; } = owner;
        public string Type => locationType?.Label() ?? "Unknown";
    }
}
