using Content.Server.Access.Systems;
using Content.Server.Popups;
using Content.Server.Radio.EntitySystems;
using Content.Server._NF.Bank;
using Content.Server._NF.Shipyard.Components;
using Content.Server._NF.ShuttleRecords;
using Content.Server._NF.Smuggling.Components;
using Content.Shared._NF.Bank.Components;
using Content.Shared._Eclipse.SelfShipyard;
using Content.Shared._Eclipse.SelfShipyard.Events;
using Content.Shared._Eclipse.SelfShipyard.BUI;
using Content.Shared._Eclipse.SelfShipyard.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Access.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Content.Shared.Radio;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Server.Maps;
using Content.Shared.StationRecords;
using Content.Server.Chat.Systems;
using Content.Server.Mind;
using Content.Server.Preferences.Managers;
using Content.Server.StationRecords;
using Content.Server.StationRecords.Systems;
using Content.Shared.Database;
using Content.Shared.Preferences;
using static Content.Shared._NF.Shipyard.Components.ShuttleDeedComponent;
using Content.Server.Shuttles.Components;
using Content.Server._NF.Station.Components;
using System.Text.RegularExpressions;
using Content.Shared.UserInterface;
using Robust.Shared.Audio.Systems;
using Content.Shared.Access;
using Content.Shared._NF.Bank.BUI;
using Content.Shared._NF.ShuttleRecords;
using Content.Server.StationEvents.Components;
using Content.Server.Forensics;
using Content.Shared.Forensics.Components;
using Content.Shared._NF.Shipyard.Components;
using Content.Shared.Ghost;
using Content.Server.Database;
using System.Threading.Tasks;
using Robust.Shared.Player;
using Content.Server._Eclipse.SelfShipyard.Components;

namespace Content.Server._Eclipse.SelfShipyard.Systems;

public sealed partial class SelfShipyardSystem : SharedSelfShipyardSystem
{
    [Dependency] private readonly AccessSystem _accessSystem = default!;
    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly IServerPreferencesManager _prefManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly BankSystem _bank = default!;
    [Dependency] private readonly IdCardSystem _idSystem = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly StationRecordsSystem _records = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly ShuttleRecordsSystem _shuttleRecordsSystem = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;

    private static readonly Regex DeedRegex = new(@"\s*\([^()]*\)");

    public void InitializeConsole()
    {

    }

    private async void OnPurchaseMessage(EntityUid shipyardConsoleUid, SelfShipyardConsoleComponent component, SelfShipyardConsolePurchaseMessage args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        if (!_playerManager.TryGetSessionByEntity(player, out var session))
        {
            //_log.Info($"OnPurchaseMessage: {player} has no attached session");
            return;
        }

        if (component.TargetIdSlot.ContainerSlot?.ContainedEntity is not { Valid: true } targetId)
        {
            ConsolePopup(player, Loc.GetString("shipyard-console-no-idcard"));
            PlayDenySound(player, shipyardConsoleUid, component);
            return;
        }

        if (!TryComp<IdCardComponent>(targetId, out var idCard))
        {
            ConsolePopup(player, Loc.GetString("shipyard-console-no-idcard"));
            PlayDenySound(player, shipyardConsoleUid, component);
            return;
        }

        if (HasComp<ShuttleDeedComponent>(targetId))
        {
            ConsolePopup(player, Loc.GetString("shipyard-console-already-deeded"));
            PlayDenySound(player, shipyardConsoleUid, component);
            return;
        }

        if (TryComp<AccessReaderComponent>(shipyardConsoleUid, out var accessReaderComponent) && !_access.IsAllowed(player, shipyardConsoleUid, accessReaderComponent))
        {
            ConsolePopup(player, Loc.GetString("comms-console-permission-denied"));
            PlayDenySound(player, shipyardConsoleUid, component);
            return;
        }

        var vessel = await _db.GetOwnedShuttle(session.UserId, args.VesselId);

        if (vessel is null)
        {
            ConsolePopup(player, Loc.GetString("shipyard-console-invalid-vessel", ("vessel", args.VesselId)));
            PlayDenySound(player, shipyardConsoleUid, component);
            return;
        }

        var name = vessel.Name;
        if (vessel.Price <= 0)
            return;

        if (_station.GetOwningStation(shipyardConsoleUid) is not { Valid: true } station)
        {
            ConsolePopup(player, Loc.GetString("shipyard-console-invalid-station"));
            PlayDenySound(player, shipyardConsoleUid, component);
            return;
        }

        if (!TryComp<BankAccountComponent>(player, out var bank))
        {
            ConsolePopup(player, Loc.GetString("shipyard-console-no-bank"));
            PlayDenySound(player, shipyardConsoleUid, component);
            return;
        }

        if (bank.Balance <= vessel.Price)
        {
            ConsolePopup(player, Loc.GetString("cargo-console-insufficient-funds", ("cost", vessel.Price)));
            PlayDenySound(player, shipyardConsoleUid, component);
            return;
        }

        if (!_bank.TryBankWithdraw(player, vessel.Price))
        {
            ConsolePopup(player, Loc.GetString("cargo-console-insufficient-funds", ("cost", vessel.Price)));
            PlayDenySound(player, shipyardConsoleUid, component);
            return;
        }


        if (!TryPurchaseShuttle(station, vessel.ShuttlePath.ToString(), out var shuttleUidOut))
        {
            PlayDenySound(player, shipyardConsoleUid, component);
            return;
        }

        await _db.RemoveOwnedShuttle(vessel.Id, session.UserId);

        var shuttleUid = shuttleUidOut.Value;
        if (!_entityManager.TryGetComponent<ShuttleComponent>(shuttleUid, out var shuttle))
        {
            PlayDenySound(player, shipyardConsoleUid, component);
            return;
        }
        EntityUid? shuttleStation = null;
        // setting up any stations if we have a matching game map prototype to allow late joins directly onto the vessel
        if (_prototypeManager.TryIndex<GameMapPrototype>(vessel.PrototypeId, out var stationProto))
        {
            List<EntityUid> gridUids = new()
            {
                shuttleUid
            };
            shuttleStation = _station.InitializeNewStation(stationProto.Stations[vessel.PrototypeId], gridUids);
            name = Name(shuttleStation.Value);
        }

        if (TryComp<AccessComponent>(targetId, out var newCap))
        {
            var newAccess = newCap.Tags.ToList();
            newAccess.AddRange(component.NewAccessLevels);
            _accessSystem.TrySetTags(targetId, newAccess, newCap);
        }

        var deedID = EnsureComp<ShuttleDeedComponent>(targetId);

        var shuttleOwner = Name(player).Trim();
        AssignShuttleDeedProperties(deedID, shuttleUid, name, shuttleOwner);

        var deedShuttle = EnsureComp<ShuttleDeedComponent>(shuttleUid);
        AssignShuttleDeedProperties(deedShuttle, shuttleUid, name, shuttleOwner);

        if (!string.IsNullOrEmpty(component.NewJobTitle))
            _idSystem.TryChangeJobTitle(targetId, component.NewJobTitle, idCard, player);

        // The following block of code is entirely to do with trying to sanely handle moving records from station to station.
        // it is ass.
        // This probably shouldnt be messed with further until station records themselves become more robust
        // and not entirely dependent upon linking ID card entity to station records key lookups
        // its just bad

        var stationList = EntityQueryEnumerator<StationRecordsComponent>();

        if (TryComp<StationRecordKeyStorageComponent>(targetId, out var keyStorage)
                && shuttleStation != null
                && keyStorage.Key != null)
        {
            bool recSuccess = false;
            while (stationList.MoveNext(out var stationUid, out var stationRecComp))
            {
                if (!_records.TryGetRecord<GeneralStationRecord>(keyStorage.Key.Value, out var record))
                    continue;

                //_records.RemoveRecord(keyStorage.Key.Value);
                _records.AddRecordEntry(shuttleStation.Value, record);
                recSuccess = true;
                break;
            }

            if (!recSuccess &&
                _mind.TryGetMind(player, out var mindUid, out var mindComp)
                && _prefManager.GetPreferences(_mind.GetSession(mindComp)!.UserId).SelectedCharacter is HumanoidCharacterProfile profile)
            {
                TryComp<FingerprintComponent>(player, out var fingerprintComponent);
                TryComp<DnaComponent>(player, out var dnaComponent);
                TryComp<StationRecordsComponent>(shuttleStation, out var stationRec);
                _records.CreateGeneralRecord(shuttleStation.Value, targetId, profile.Name, profile.Age, profile.Species, profile.Gender, $"Captain", fingerprintComponent!.Fingerprint, dnaComponent!.DNA, profile, stationRec!);
            }
        }
        _records.Synchronize(shuttleStation!.Value);
        _records.Synchronize(station);

        // Ensure cleanup on ship sale
        EnsureComp<LinkedLifecycleGridParentComponent>(shuttleUid);

        var saveCost = 0;
        // Get the price of the ship
        if (TryComp<ShuttleDeedComponent>(targetId, out var deed))
            saveCost = (int)_pricing.AppraiseGrid((EntityUid)(deed?.ShuttleUid!), LacksPreserveOnSaleComp);

        saveCost = (int)(saveCost * _percentSaveRate) + _constantSaveRate;

        SendPurchaseMessage(shipyardConsoleUid, player, name, component.ShipyardChannel, secret: false);
        if (component.SecretShipyardChannel is { } secretChannel)
            SendPurchaseMessage(shipyardConsoleUid, player, name, secretChannel, secret: true);

        PlayConfirmSound(player, shipyardConsoleUid, component);
        _adminLogger.Add(LogType.ShipYardUsage, LogImpact.Low, $"{ToPrettyString(player):actor} used {ToPrettyString(targetId)} to purchase shuttle {ToPrettyString(shuttleUid)} for {vessel.Price} credits via {ToPrettyString(shipyardConsoleUid)}");

        await RefreshState(shipyardConsoleUid, player, bank.Balance, true, name, saveCost, targetId, (SelfShipyardConsoleUiKey)args.UiKey);
    }

    private void TryParseShuttleName(ShuttleDeedComponent deed, string name)
    {
        // The logic behind this is: if a name part fits the requirements, it is the required part. Otherwise it's the name.
        // This may cause problems but ONLY when renaming a ship. It will still display properly regardless of this.
        var nameParts = name.Split(' ');

        var hasSuffix = nameParts.Length > 1 && nameParts.Last().Length < MaxSuffixLength && nameParts.Last().Contains('-');
        deed.ShuttleNameSuffix = hasSuffix ? nameParts.Last() : null;
        deed.ShuttleName = String.Join(" ", nameParts.SkipLast(hasSuffix ? 1 : 0));
    }

    public async void OnSellMessage(EntityUid uid, SelfShipyardConsoleComponent component, SelfShipyardConsoleSellMessage args)
    {

        if (args.Actor is not { Valid: true } player)
            return;

        if (!_playerManager.TryGetSessionByEntity(player, out var session))
        {
            //_log.Info($"OnPurchaseMessage: {player} has no attached session");
            return;
        }

        if (component.TargetIdSlot.ContainerSlot?.ContainedEntity is not { Valid: true } targetId)
        {
            ConsolePopup(player, Loc.GetString("shipyard-console-no-idcard"));
            PlayDenySound(player, uid, component);
            return;
        }

        if (TryComp<ShipyardVoucherComponent>(targetId, out var _))
        {
            ConsolePopup(player, Loc.GetString("self-shipyard-console-not-accepting-voucher"));
            PlayDenySound(player, uid, component);
            return;
        }

        if (!TryComp<IdCardComponent>(targetId, out var idCard))
        {
            ConsolePopup(player, Loc.GetString("shipyard-console-no-idcard"));
            PlayDenySound(player, uid, component);
            return;
        }

        if (!TryComp<ShuttleDeedComponent>(targetId, out var deed) || deed.ShuttleUid is not { Valid: true } shuttleUid)
        {
            ConsolePopup(player, Loc.GetString("shipyard-console-no-deed"));
            PlayDenySound(player, uid, component);
            return;
        }

        if (!TryComp<SaveableShuttleComponent>(shuttleUid, out var saveableShuttle))
        {
            ConsolePopup(player, Loc.GetString("self-shipyard-console-save-not-allowed"));
            PlayDenySound(player, uid, component);
            return;
        }

        if (!TryComp<BankAccountComponent>(player, out var bank))
        {
            ConsolePopup(player, Loc.GetString("shipyard-console-no-bank"));
            PlayDenySound(player, uid, component);
            return;
        }

        if (_station.GetOwningStation(uid) is not { Valid: true } stationUid)
        {
            ConsolePopup(player, Loc.GetString("shipyard-console-invalid-station"));
            PlayDenySound(player, uid, component);
            return;
        }

        if (_station.GetOwningStation(shuttleUid) is { Valid: true } shuttleStation
            && TryComp<StationRecordKeyStorageComponent>(targetId, out var keyStorage)
            && keyStorage.Key != null
            && keyStorage.Key.Value.OriginStation == shuttleStation
            && _records.TryGetRecord<GeneralStationRecord>(keyStorage.Key.Value, out var record))
        {
            //_records.RemoveRecord(keyStorage.Key.Value);
            _records.AddRecordEntry(stationUid, record);
            _records.Synchronize(stationUid);
        }

        var shuttleName = ToPrettyString(shuttleUid); // Grab the name before it gets 1984'd

        var (saleResult, bill) = await TrySaveShuttle(player, session, stationUid, shuttleUid, uid, saveableShuttle);
        if (saleResult.Error != ShipyardSaleError.Success)
        {
            switch (saleResult.Error)
            {
                case ShipyardSaleError.Undocked:
                    ConsolePopup(player, Loc.GetString("shipyard-console-sale-not-docked"));
                    break;
                case ShipyardSaleError.OrganicsAboard:
                    ConsolePopup(player, Loc.GetString("shipyard-console-sale-organic-aboard", ("name", saleResult.OrganicName ?? "Somebody")));
                    break;
                case ShipyardSaleError.InvalidShip:
                    ConsolePopup(player, Loc.GetString("shipyard-console-sale-invalid-ship"));
                    break;
                case ShipyardSaleError.InsufficientFunds:
                    ConsolePopup(player, Loc.GetString("cargo-console-insufficient-funds", ("cost", bill)));
                    break;
                default:
                    ConsolePopup(player, Loc.GetString("shipyard-console-sale-unknown-reason", ("reason", saleResult.Error.ToString())));
                    break;
            }
            PlayDenySound(player, uid, component);
            return;
        }

        RemComp<ShuttleDeedComponent>(targetId);

        PlayConfirmSound(player, uid, component);

        var name = GetFullName(deed);
        SendSellMessage(uid, deed.ShuttleOwner!, name, component.ShipyardChannel, player, secret: false);
        if (component.SecretShipyardChannel is { } secretChannel)
            SendSellMessage(uid, deed.ShuttleOwner!, name, secretChannel, player, secret: true);

        EntityUid? refreshId = targetId;

        _adminLogger.Add(LogType.ShipYardUsage, LogImpact.Low, $"{ToPrettyString(player):actor} used {ToPrettyString(targetId)} to sell {shuttleName} for {bill} credits via {ToPrettyString(uid)}");

        await RefreshState(uid, player, bank.Balance, true, null, 0, refreshId, (SelfShipyardConsoleUiKey)args.UiKey);
    }

    private async void OnConsoleUIOpened(EntityUid uid, SelfShipyardConsoleComponent component, BoundUIOpenedEvent args)
    {
        if (!component.Initialized)
            return;

        // kind of cursed. We need to update the UI when an Id is entered, but the UI needs to know the player characters bank account.
        if (!TryComp<ActivatableUIComponent>(uid, out var uiComp) || uiComp.Key == null)
            return;

        if (args.Actor is not { Valid: true } player)
            return;

        //      mayhaps re-enable this later for HoS/SA
        //        var station = _station.GetOwningStation(uid);

        if (!TryComp<BankAccountComponent>(player, out var bank))
            return;

        var targetId = component.TargetIdSlot.ContainerSlot?.ContainedEntity;

        if (TryComp<ShuttleDeedComponent>(targetId, out var deed))
        {
            if (Deleted(deed!.ShuttleUid))
            {
                RemComp<ShuttleDeedComponent>(targetId!.Value);
                return;
            }
        }

        int saveCost = 0;
        if (deed?.ShuttleUid != null)
        {
            saveCost = (int)_pricing.AppraiseGrid((EntityUid)(deed?.ShuttleUid!), LacksPreserveOnSaleComp);
            saveCost = (int)(saveCost * _percentSaveRate) + _constantSaveRate;
        }

        var fullName = deed != null ? GetFullName(deed) : null;
        await RefreshState(uid, player, bank.Balance, true, fullName, saveCost, targetId, (SelfShipyardConsoleUiKey)args.UiKey);
    }

    private void ConsolePopup(EntityUid uid, string text)
    {
        _popup.PopupEntity(text, uid);
    }

    private void SendPurchaseMessage(EntityUid uid, EntityUid player, string name, string shipyardChannel, bool secret)
    {
        var channel = _prototypeManager.Index<RadioChannelPrototype>(shipyardChannel);

        if (secret)
        {
            _radio.SendRadioMessage(uid, Loc.GetString("shipyard-console-docking-secret"), channel, uid);
            _chat.TrySendInGameICMessage(uid, Loc.GetString("shipyard-console-docking-secret"), InGameICChatType.Speak, true);
        }
        else
        {
            _radio.SendRadioMessage(uid, Loc.GetString("shipyard-console-docking", ("owner", player), ("vessel", name)), channel, uid);
            _chat.TrySendInGameICMessage(uid, Loc.GetString("shipyard-console-docking", ("owner", player!), ("vessel", name)), InGameICChatType.Speak, true);
        }
    }

    private void SendSellMessage(EntityUid uid, string? player, string name, string shipyardChannel, EntityUid seller, bool secret)
    {
        var channel = _prototypeManager.Index<RadioChannelPrototype>(shipyardChannel);

        if (secret)
        {
            _radio.SendRadioMessage(uid, Loc.GetString("shipyard-console-leaving-secret"), channel, uid);
            _chat.TrySendInGameICMessage(uid, Loc.GetString("shipyard-console-leaving-secret"), InGameICChatType.Speak, true);
        }
        else
        {
            _radio.SendRadioMessage(uid, Loc.GetString("shipyard-console-leaving", ("owner", player!), ("vessel", name!), ("player", seller)), channel, uid);
            _chat.TrySendInGameICMessage(uid, Loc.GetString("shipyard-console-leaving", ("owner", player!), ("vessel", name!), ("player", seller)), InGameICChatType.Speak, true);
        }
    }

    private void PlayDenySound(EntityUid playerUid, EntityUid consoleUid, SelfShipyardConsoleComponent component)
    {
        _audio.PlayEntity(component.ErrorSound, playerUid, consoleUid);
    }

    private void PlayConfirmSound(EntityUid playerUid, EntityUid consoleUid, SelfShipyardConsoleComponent component)
    {
        _audio.PlayEntity(component.ConfirmSound, playerUid, consoleUid);
    }

    private async void OnItemSlotChanged(EntityUid uid, SelfShipyardConsoleComponent component, ContainerModifiedMessage args)
    {
        if (!component.Initialized)
            return;

        if (args.Container.ID != component.TargetIdSlot.ID)
            return;

        // kind of cursed. We need to update the UI when an Id is entered, but the UI needs to know the player characters bank account.
        if (!TryComp<ActivatableUIComponent>(uid, out var uiComp) || uiComp.Key == null)
            return;

        var uiUsers = _ui.GetActors(uid, uiComp.Key);

        foreach (var user in uiUsers)
        {
            if (user is not { Valid: true } player)
                continue;

            if (!TryComp<BankAccountComponent>(player, out var bank))
                continue;

            var targetId = component.TargetIdSlot.ContainerSlot?.ContainedEntity;

            if (TryComp<ShuttleDeedComponent>(targetId, out var deed))
            {
                if (Deleted(deed!.ShuttleUid))
                {
                    RemComp<ShuttleDeedComponent>(targetId!.Value);
                    continue;
                }
            }

            int saveCost = 0;
            if (deed?.ShuttleUid != null)
            {
                saveCost = (int)_pricing.AppraiseGrid(deed.ShuttleUid.Value, LacksPreserveOnSaleComp);
                saveCost = (int)(saveCost * _percentSaveRate) + _constantSaveRate;
            }

            var fullName = deed != null ? GetFullName(deed) : null;
            await RefreshState(uid,
                player,
                bank.Balance,
                true,
                fullName,
                saveCost,
                targetId,
                (SelfShipyardConsoleUiKey)uiComp.Key);

        }
    }

    /// <summary>
    /// Looks for a living, sapient being aboard a particular entity.
    /// </summary>
    /// <param name="uid">The entity to search (e.g. a shuttle, a station)</param>
    /// <param name="mobQuery">A query to get the MobState from an entity</param>
    /// <param name="xformQuery">A query to get the transform component of an entity</param>
    /// <returns>The name of the sapient being if one was found, null otherwise.</returns>
    public string? FoundOrganics(EntityUid uid, EntityQuery<MobStateComponent> mobQuery, EntityQuery<TransformComponent> xformQuery)
    {
        var xform = xformQuery.GetComponent(uid);
        var childEnumerator = xform.ChildEnumerator;

        while (childEnumerator.MoveNext(out var child))
        {
            // Ghosts don't stop a ship sale.
            if (HasComp<GhostComponent>(child))
                continue;

            // Check if we have a player entity that's either still around or alive and may come back
            if (_mind.TryGetMind(child, out var mind, out var mindComp)
                && (mindComp.Session != null
                || !_mind.IsCharacterDeadPhysically(mindComp)))
            {
                return Name(child);
            }
            else
            {
                var charName = FoundOrganics(child, mobQuery, xformQuery);
                if (charName != null)
                    return charName;
            }
        }

        return null;
    }

    private struct IDShipAccesses
    {
        public IReadOnlyCollection<ProtoId<AccessLevelPrototype>> Tags;
        public IReadOnlyCollection<ProtoId<AccessGroupPrototype>> Groups;
    }

    /// <summary>
    ///   Returns all owned shuttle records for the provided player
    /// </summary>
    public async Task<List<OwnedVesselVisibleRecord>> GetAvailableShuttles(EntityUid uid, EntityUid playerUid, EntityUid? targetId = null)
    {
        var available = new List<OwnedVesselVisibleRecord>();

        if (!_playerManager.TryGetSessionByEntity(playerUid, out var session))
        {
            //_log.Info($"GetAvailableShuttles: {playerUid} has no attached session");
            return [];
        }

        foreach (var vessel in await _db.GetOwnedShuttlesForPlayer(session.UserId))
        {
            available.Add(new OwnedVesselVisibleRecord(vessel.Id, vessel.Name, vessel.Description, vessel.Price));
        }

        return available;
    }

    private async Task RefreshState(EntityUid uid, EntityUid player, int balance, bool access, string? shipDeed, int shipSaveRate, EntityUid? targetId, SelfShipyardConsoleUiKey uiKey)
    {
        var newState = new SelfShipyardConsoleInterfaceState(
            balance,
            access,
            shipDeed,
            shipSaveRate,
            targetId.HasValue,
            ((byte)uiKey),
            await GetAvailableShuttles(uid, player, targetId: targetId),
            uiKey.ToString(),
            _percentSaveRate,
            _constantSaveRate);

        _ui.SetUiState(uid, uiKey, newState);
    }

    #region Deed Assignment
    void AssignShuttleDeedProperties(ShuttleDeedComponent deed, EntityUid? shuttleUid, string? shuttleName, string? shuttleOwner)
    {
        deed.ShuttleUid = shuttleUid;
        TryParseShuttleName(deed, shuttleName!);
        deed.ShuttleOwner = shuttleOwner;
        deed.PurchasedWithVoucher = false;
    }
    #endregion
}
