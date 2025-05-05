using Content.Shared._Eclipse.Implants.Components;
using Content.Shared.Foldable;
using Content.Shared.Implants;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._Eclipse.Implants.EntitySystems
{
    public abstract partial class SharedEmergencyReturnImplantSystem : EntitySystem
    {
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly FoldableSystem _foldable = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<ReturnImplantConnectComponent, AfterInteractEvent>(OnBeaconInteract);
            SubscribeLocalEvent<ReturnImplantConnectComponent, ImplanterAttemptEvent>(OnAttemptImplant);
        }

        private void OnBeaconInteract(EntityUid uid, ReturnImplantConnectComponent component, AfterInteractEvent args)
        {
            if (args.Target == null || args.Handled || !args.CanReach)
                return;

            if (!TryComp<ReturnImplantBeaconComponent>(args.Target, out var beacon))
                return;

            if (!_foldable.IsFolded(args.Target.Value))
            {
                component.Beacon = args.Target.Value;
                _audio.PlayEntity(beacon.LinkSound, uid, args.User);
                _popup.PopupEntity(Loc.GetString("return-implant-beacon-linked"), uid, args.User);
            }
            else
            {
                component.Beacon = EntityUid.Invalid;
                _popup.PopupEntity(Loc.GetString("return-implant-beacon-folded"), uid, args.User);
            }

            args.Handled = true;
        }

        private void OnAttemptImplant(EntityUid uid, ReturnImplantConnectComponent component, ImplanterAttemptEvent args)
        {
            if (args.Cancelled)
                return;

            if (TryComp(args.Implant, out TransportToBeaconOnTriggerComponent? comp) && comp.Used)
            {
                _popup.PopupEntity(Loc.GetString("return-implant-used"), uid, args.User);
                args.Cancel();
                return;
            }

            if (component.Beacon == null || component.Beacon == EntityUid.Invalid)
            {
                _popup.PopupEntity(Loc.GetString("return-implant-beacon-not-set"), uid, args.User);
                args.Cancel();
            }
        }
    }
}
