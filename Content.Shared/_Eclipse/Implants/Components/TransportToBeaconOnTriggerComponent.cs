using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Eclipse.Implants.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class TransportToBeaconOnTriggerComponent : Component
{
    [DataField]
    public EntProtoId FlashPrototype = "EffectFlashBluespace";

    [ViewVariables(VVAccess.ReadWrite), DataField("beacon")]
    public EntityUid? Beacon;

    [DataField]
    public bool Used = false;
}