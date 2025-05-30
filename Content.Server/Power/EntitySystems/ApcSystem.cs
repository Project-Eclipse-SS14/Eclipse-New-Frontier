using Content.Server.Emp;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Power.Pow3r;
using Content.Shared.Access.Systems;
using Content.Shared.APC;
using Content.Shared.Emag.Systems;
using Content.Shared.Emp; // Frontier: Upstream - #28984
using Content.Shared.Popups;
using Content.Shared.Rounding;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
using Content.Shared.Tools.Components;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.DeviceNetwork;

namespace Content.Server.Power.EntitySystems;

public sealed class ApcSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(PowerNetSystem));

        SubscribeLocalEvent<ApcComponent, BoundUIOpenedEvent>(OnBoundUiOpen);
        SubscribeLocalEvent<ApcComponent, ComponentStartup>(OnApcStartup);
        SubscribeLocalEvent<ApcComponent, ChargeChangedEvent>(OnBatteryChargeChanged);
        SubscribeLocalEvent<ApcComponent, ApcToggleMainBreakerMessage>(OnToggleMainBreaker);
        SubscribeLocalEvent<ApcComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<ApcComponent, GotUnEmaggedEvent>(OnUnemagged); // Frontier

        SubscribeLocalEvent<ApcComponent, EmpPulseEvent>(OnEmpPulse);
        SubscribeLocalEvent<ApcComponent, EmpDisabledRemoved>(OnEmpDisabledRemoved); // Frontier: Upstream - #28984
        SubscribeLocalEvent<ApcComponent, ToolUseAttemptEvent>(OnToolUseAttempt); // Frontier

        SubscribeLocalEvent<ApcComponent, SignalReceivedEvent>(OnSignalReceived); // Eclipse
        SubscribeLocalEvent<ApcComponent, DeviceNetworkPacketEvent>(OnPacketReceived); // Eclipse
    }

    public override void Update(float deltaTime)
    {
        var query = EntityQueryEnumerator<ApcComponent, PowerNetworkBatteryComponent, UserInterfaceComponent>();
        while (query.MoveNext(out var uid, out var apc, out var battery, out var ui))
        {
            if (apc.LastUiUpdate + ApcComponent.VisualsChangeDelay < _gameTiming.CurTime && _ui.IsUiOpen((uid, ui), ApcUiKey.Key))
            {
                apc.LastUiUpdate = _gameTiming.CurTime;
                UpdateUIState(uid, apc, battery);
            }

            if (apc.NeedStateUpdate)
            {
                UpdateApcState(uid, apc, battery);
            }
        }
    }

    // Change the APC's state only when the battery state changes, or when it's first created.
    private void OnBatteryChargeChanged(EntityUid uid, ApcComponent component, ref ChargeChangedEvent args)
    {
        UpdateApcState(uid, component);
    }

    private static void OnApcStartup(EntityUid uid, ApcComponent component, ComponentStartup args)
    {
        // We cannot update immediately, as various network/battery state is not valid yet.
        // Defer until the next tick.
        component.NeedStateUpdate = true;
    }

    private void OnBoundUiOpen(EntityUid uid, ApcComponent component, BoundUIOpenedEvent args)
    {
        UpdateApcState(uid, component);
    }

    private void OnToggleMainBreaker(EntityUid uid, ApcComponent component, ApcToggleMainBreakerMessage args)
    {
        var attemptEv = new ApcToggleMainBreakerAttemptEvent();
        RaiseLocalEvent(uid, ref attemptEv);
        if (attemptEv.Cancelled)
        {
            _popup.PopupCursor(Loc.GetString("apc-component-on-toggle-cancel"),
                args.Actor, PopupType.Medium);
            return;
        }

        if (_accessReader.IsAllowed(args.Actor, uid))
        {
            ApcToggleBreaker(uid, component);
        }
        else
        {
            _popup.PopupCursor(Loc.GetString("apc-component-insufficient-access"),
                args.Actor, PopupType.Medium);
        }
    }

    public void ApcSetBreaker(EntityUid uid, bool enabled, ApcComponent? apc = null, PowerNetworkBatteryComponent? battery = null)
    {
        if (!Resolve(uid, ref apc, ref battery))
            return;

        apc.MainBreakerEnabled = enabled;
        battery.CanDischarge = apc.MainBreakerEnabled;

        UpdateUIState(uid, apc);
        _audio.PlayPvs(apc.OnReceiveMessageSound, uid, AudioParams.Default.WithVolume(-2f));
    }

    public void ApcToggleBreaker(EntityUid uid, ApcComponent? apc = null, PowerNetworkBatteryComponent? battery = null)
    {
        if (!Resolve(uid, ref apc, ref battery))
            return;

        apc.MainBreakerEnabled = !apc.MainBreakerEnabled;
        battery.CanDischarge = apc.MainBreakerEnabled;

        UpdateUIState(uid, apc);
        _audio.PlayPvs(apc.OnReceiveMessageSound, uid, AudioParams.Default.WithVolume(-2f));
    }

    private void OnEmagged(EntityUid uid, ApcComponent comp, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(uid, EmagType.Interaction))
            return;

        args.Handled = true;
    }

    // Frontier: demag
    private void OnUnemagged(EntityUid uid, ApcComponent comp, ref GotUnEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (!_emag.CheckFlag(uid, EmagType.Interaction))
            return;

        args.Handled = true;
    }
    // End Frontier

    public void UpdateApcState(EntityUid uid,
        ApcComponent? apc = null,
        PowerNetworkBatteryComponent? battery = null)
    {
        if (!Resolve(uid, ref apc, ref battery, false))
            return;

        if (apc.LastChargeStateTime == null || apc.LastChargeStateTime + ApcComponent.VisualsChangeDelay < _gameTiming.CurTime)
        {
            var newState = CalcChargeState(uid, battery.NetworkBattery);
            if (newState != apc.LastChargeState)
            {
                apc.LastChargeState = newState;
                apc.LastChargeStateTime = _gameTiming.CurTime;

                if (TryComp(uid, out AppearanceComponent? appearance))
                {
                    _appearance.SetData(uid, ApcVisuals.ChargeState, newState, appearance);
                }
            }
        }

        var extPowerState = CalcExtPowerState(uid, battery.NetworkBattery);
        if (extPowerState != apc.LastExternalState)
        {
            apc.LastExternalState = extPowerState;
            UpdateUIState(uid, apc, battery);
        }

        apc.NeedStateUpdate = false;
    }

    public void UpdateUIState(EntityUid uid,
        ApcComponent? apc = null,
        PowerNetworkBatteryComponent? netBat = null,
        UserInterfaceComponent? ui = null)
    {
        if (!Resolve(uid, ref apc, ref netBat, ref ui))
            return;

        var battery = netBat.NetworkBattery;
        const int ChargeAccuracy = 5;

        // TODO: Fix ContentHelpers or make a new one coz this is cooked.
        var charge = ContentHelpers.RoundToNearestLevels(battery.CurrentStorage / battery.Capacity, 1.0, 100 / ChargeAccuracy) / 100f * ChargeAccuracy;

        var state = new ApcBoundInterfaceState(apc.MainBreakerEnabled,
            (int)MathF.Ceiling(battery.CurrentSupply), apc.LastExternalState,
            charge);

        _ui.SetUiState((uid, ui), ApcUiKey.Key, state);
    }

    private ApcChargeState CalcChargeState(EntityUid uid, PowerState.Battery battery)
    {
        if (_emag.CheckFlag(uid, EmagType.Interaction) || HasComp<EmpDisabledComponent>(uid)) // Frontier: Upstream - #28984: add HasComp
            return ApcChargeState.Emag;

        if (battery.CurrentStorage / battery.Capacity > ApcComponent.HighPowerThreshold)
        {
            return ApcChargeState.Full;
        }

        var delta = battery.CurrentSupply - battery.CurrentReceiving;
        return delta < 0 ? ApcChargeState.Charging : ApcChargeState.Lack;
    }

    private ApcExternalPowerState CalcExtPowerState(EntityUid uid, PowerState.Battery battery)
    {
        if (battery.CurrentReceiving == 0 && !MathHelper.CloseTo(battery.CurrentStorage / battery.Capacity, 1))
        {
            return ApcExternalPowerState.None;
        }

        var delta = battery.CurrentSupply - battery.CurrentReceiving;
        if (!MathHelper.CloseToPercent(delta, 0, 0.1f) && delta < 0)
        {
            return ApcExternalPowerState.Low;
        }

        return ApcExternalPowerState.Good;
    }
    private void OnEmpPulse(EntityUid uid, ApcComponent component, ref EmpPulseEvent args) // Frontier: Upstream - #28984
    {
        //if (component.MainBreakerEnabled)
        //{
        //    args.Affected = true;
        //    args.Disabled = true;
        //    ApcToggleBreaker(uid, component);
        //}
        EnsureComp<EmpDisabledComponent>(uid, out var emp); //event calls before EmpDisabledComponent is added, ensure it to force sprite update
        UpdateApcState(uid);
    }

    private void OnEmpDisabledRemoved(EntityUid uid, ApcComponent component, ref EmpDisabledRemoved args) // Frontier: Upstream - #28984
    {
        UpdateApcState(uid);
    }

    private void OnToolUseAttempt(EntityUid uid, ApcComponent component, ToolUseAttemptEvent args) // Frontier
    {
        if (!HasComp<EmpDisabledComponent>(uid))
            return;

        foreach (var quality in args.Qualities)
        {
            // prevent reconstruct exploit to skip cooldowns
            if (quality == "Prying")
            {
                args.Cancel();
                return;
            }
        }
    }

    private void OnSignalReceived(EntityUid uid, ApcComponent component, ref SignalReceivedEvent args)
    {
        if (args.Port == component.OffPort)
            ApcSetBreaker(uid, false, component);
        else if (args.Port == component.OnPort)
            ApcSetBreaker(uid, true, component);
        else if (args.Port == component.TogglePort)
            ApcToggleBreaker(uid, component);
    }

    private void OnPacketReceived(EntityUid uid, ApcComponent component, DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(DeviceNetworkConstants.Command, out string? command) || command != DeviceNetworkConstants.CmdSetState) return;
        if (!args.Data.TryGetValue(DeviceNetworkConstants.StateEnabled, out bool enabled)) return;

        ApcSetBreaker(uid, enabled, component);
    }
}

[ByRefEvent]
public record struct ApcToggleMainBreakerAttemptEvent(bool Cancelled);
