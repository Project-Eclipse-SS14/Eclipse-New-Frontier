using Robust.Shared.Serialization;

namespace Content.Shared._Eclipse.SelfShipyard.Events;

/// <summary>
///     Purchase a Vessel from the console
/// </summary>
[Serializable, NetSerializable]
public sealed class SelfShipyardConsolePurchaseMessage : BoundUserInterfaceMessage
{
    public int VesselId; //vessel record id

    public SelfShipyardConsolePurchaseMessage(int vesselId)
    {
        VesselId = vesselId;
    }
}
