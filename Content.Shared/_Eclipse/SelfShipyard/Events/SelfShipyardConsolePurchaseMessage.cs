using Robust.Shared.Serialization;

namespace Content.Shared._Eclipse.SelfShipyard.Events;

/// <summary>
///     Purchase a Vessel from the console
/// </summary>
[Serializable, NetSerializable]
public sealed class SelfShipyardConsolePurchaseMessage : BoundUserInterfaceMessage
{
    public string Vessel; //vessel prototype ID

    public SelfShipyardConsolePurchaseMessage(string vessel)
    {
        Vessel = vessel;
    }
}
