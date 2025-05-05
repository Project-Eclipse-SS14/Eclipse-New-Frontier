using System.Numerics;
using Content.Server._Eclipse.Implants.Components;
using Content.Server.Explosion.EntitySystems;
using Content.Shared._Eclipse.Implants;
using Content.Shared._Eclipse.Implants.Components;
using Content.Shared._Eclipse.Implants.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server._Eclipse.Implants.EntitySystems
{
    public sealed class EmergencyReturnImplantSystem : SharedEmergencyReturnImplantSystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
        [Dependency] private readonly TriggerSystem _trigger = default!;
        [Dependency] private readonly SharedContainerSystem _container = default!;
        [Dependency] private readonly SharedSubdermalImplantSystem _implantSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<TriggerOnExpeditionNearEndComponent, ExpeditionNearEndEvent>(OnExpeditionNearEnd);
            SubscribeLocalEvent<TriggerOnExpeditionNearEndComponent, ImplantRelayEvent<ExpeditionNearEndEvent>>(OnExpeditionNearEndRelay);
            SubscribeLocalEvent<TransportToBeaconOnTriggerComponent, TriggerEvent>(HandleTransportToShuttleTrigger);
            SubscribeLocalEvent<TransportToBeaconOnTriggerComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<TransportToBeaconOnTriggerComponent, ImplantImplantedByEvent>(OnImplanted);
        }

        private void OnExpeditionNearEnd(EntityUid uid, TriggerOnExpeditionNearEndComponent component, ExpeditionNearEndEvent args)
        {
            _trigger.Trigger(uid);
        }

        private void OnExpeditionNearEndRelay(EntityUid uid, TriggerOnExpeditionNearEndComponent component, ImplantRelayEvent<ExpeditionNearEndEvent> args)
        {
            _trigger.Trigger(uid);
        }

        private void OnExamined(EntityUid uid, TransportToBeaconOnTriggerComponent component, ExaminedEvent args)
        {
            if (component.Used)
                args.PushMarkup(Loc.GetString("return-implant-used"));
        }

        private void HandleTransportToShuttleTrigger(EntityUid uid, TransportToBeaconOnTriggerComponent component, TriggerEvent args)
        {
            if (!TryComp<SubdermalImplantComponent>(uid, out var implanted))
                return;

            if (implanted.ImplantedEntity == null)
                return;

            if (component.Used)
                return;

            if (!Deleted(component.Beacon) &&
                TryComp(component.Beacon, out TransformComponent? beaconXform) &&
                !_container.IsEntityOrParentInContainer(component.Beacon.Value, xform: beaconXform) &&
                CanTeleport(uid)
                && TryComp(implanted.ImplantedEntity, out TransformComponent? implantedEntityXform))
            {
                if (beaconXform.ParentUid == implantedEntityXform.ParentUid)
                    return;

                var offset = _random.NextVector2(1.5f);
                var localPos = Vector2.Transform(
                        _transformSystem.GetWorldPosition(beaconXform),
                        _transformSystem.GetInvWorldMatrix(beaconXform.ParentUid)) + offset;

                var targetCoords = new EntityCoordinates(beaconXform.ParentUid, localPos);

                _transformSystem.SetCoordinates(implanted.ImplantedEntity.Value, targetCoords);
                Spawn(component.FlashPrototype, targetCoords);
                component.Used = true;
                Dirty(uid, component);
            }

            args.Handled = true;
        }

        private bool CanTeleport(EntityUid uid)
        {
            var xform = Transform(uid);

            if (xform.Anchored)
                return false;

            return true;
        }

        private void OnImplanted(EntityUid uid, TransportToBeaconOnTriggerComponent component, ImplantImplantedByEvent args)
        {
            if (!TryComp<ReturnImplantConnectComponent>(args.Implanter, out var implantComp))
                return;

            component.Beacon = implantComp.Beacon;
        }
    }
}