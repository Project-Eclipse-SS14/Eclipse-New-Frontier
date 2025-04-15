using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry
{
    [Serializable, NetSerializable]
    public sealed class ChemMasterStartGrinderMessage : BoundUserInterfaceMessage
    {
        public ChemMasterStartGrinderMessage()
        {
        }
    }

    [Serializable, NetSerializable]
    public sealed class ChemMasterEjectGrinderChamberAllMessage : BoundUserInterfaceMessage
    {
        public ChemMasterEjectGrinderChamberAllMessage()
        {
        }
    }

    [Serializable, NetSerializable]
    public sealed class ChemMasterEjectGrinderChamberContentMessage : BoundUserInterfaceMessage
    {
        public NetEntity EntityId;
        public ChemMasterEjectGrinderChamberContentMessage(NetEntity entityId)
        {
            EntityId = entityId;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ChemMasterBotanyBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly ContainerInfo? InputContainerInfo;
        public readonly ContainerInfo? OutputContainerInfo;

        /// <summary>
        /// A list of the reagents and their amounts within the buffer, if applicable.
        /// </summary>
        public readonly IReadOnlyList<ReagentQuantity> BufferReagents;

        public readonly ChemMasterMode Mode;

        public readonly ChemMasterSortingType SortingType;

        public readonly FixedPoint2? BufferCurrentVolume;
        public readonly uint SelectedPillType;

        public readonly uint PillDosageLimit;

        public readonly bool UpdateLabel;
        public NetEntity[] ChamberContents;

        public ChemMasterBotanyBoundUserInterfaceState(
            ChemMasterMode mode, ChemMasterSortingType sortingType, ContainerInfo? inputContainerInfo, ContainerInfo? outputContainerInfo,
            IReadOnlyList<ReagentQuantity> bufferReagents, FixedPoint2 bufferCurrentVolume,
            uint selectedPillType, uint pillDosageLimit, bool updateLabel, NetEntity[] chamberContents)
        {
            InputContainerInfo = inputContainerInfo;
            OutputContainerInfo = outputContainerInfo;
            BufferReagents = bufferReagents;
            Mode = mode;
            SortingType = sortingType;
            BufferCurrentVolume = bufferCurrentVolume;
            SelectedPillType = selectedPillType;
            PillDosageLimit = pillDosageLimit;
            UpdateLabel = updateLabel;
            ChamberContents = chamberContents;
        }
    }

    [Serializable, NetSerializable]
    public enum ChemMasterBotanyUiKey
    {
        Key
    }
}