using Robust.Shared.GameStates;

namespace Content.Shared._Eclipse.Implants.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ReturnImplantConnectComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("beacon")]
    public EntityUid? Beacon;
}