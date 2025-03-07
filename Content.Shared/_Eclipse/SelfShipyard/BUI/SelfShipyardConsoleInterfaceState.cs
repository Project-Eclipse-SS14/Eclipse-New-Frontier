using Robust.Shared.Serialization;

namespace Content.Shared._Eclipse.SelfShipyard.BUI;

[NetSerializable, Serializable]
public sealed class SelfShipyardConsoleInterfaceState : BoundUserInterfaceState
{
    public int Balance;
    public readonly bool AccessGranted;
    public readonly string? ShipDeedTitle;
    public int ShipSaveRate;
    public readonly bool IsTargetIdPresent;
    public readonly byte UiKey;

    public readonly (List<string> available, List<string> unavailable) ShipyardPrototypes;
    public readonly string ShipyardName;
    public readonly float PercentSellRate;
    public readonly int ConstantSellRate;

    public SelfShipyardConsoleInterfaceState(
        int balance,
        bool accessGranted,
        string? shipDeedTitle,
        int shipSaveRate,
        bool isTargetIdPresent,
        byte uiKey,
        (List<string> available, List<string> unavailable) shipyardPrototypes,
        string shipyardName,
        float percentSellRate,
        int constantSellRate)
    {
        Balance = balance;
        AccessGranted = accessGranted;
        ShipDeedTitle = shipDeedTitle;
        ShipSaveRate = shipSaveRate;
        IsTargetIdPresent = isTargetIdPresent;
        UiKey = uiKey;
        ShipyardPrototypes = shipyardPrototypes;
        ShipyardName = shipyardName;
        PercentSellRate = percentSellRate;
        ConstantSellRate = constantSellRate;
    }
}
