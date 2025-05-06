using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Ranged.Events;

[ByRefEvent]
public sealed class GetAmmoProtoEvent
{
    public EntProtoId? AmmoProto;

    public GetAmmoProtoEvent(EntProtoId? ammoProto)
    {
        AmmoProto = ammoProto;
    }
}
