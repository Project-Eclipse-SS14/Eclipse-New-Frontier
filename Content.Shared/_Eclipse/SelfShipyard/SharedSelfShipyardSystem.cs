using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Serialization;
using Content.Shared._Eclipse.SelfShipyard.Components;

namespace Content.Shared._Eclipse.SelfShipyard;

[NetSerializable, Serializable]
public enum SelfShipyardConsoleUiKey : byte
{
    Key
}

public abstract class SharedSelfShipyardSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SelfShipyardConsoleComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<SelfShipyardConsoleComponent, ComponentRemove>(OnComponentRemove);
    }

    private void OnComponentInit(EntityUid uid, SelfShipyardConsoleComponent component, ComponentInit args)
    {
        _itemSlotsSystem.AddItemSlot(uid, SelfShipyardConsoleComponent.TargetIdCardSlotId, component.TargetIdSlot);
    }

    private void OnComponentRemove(EntityUid uid, SelfShipyardConsoleComponent component, ComponentRemove args)
    {
        _itemSlotsSystem.RemoveItemSlot(uid, component.TargetIdSlot);
    }
}
