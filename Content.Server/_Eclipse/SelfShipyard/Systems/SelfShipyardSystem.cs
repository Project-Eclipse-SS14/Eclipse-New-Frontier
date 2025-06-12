using Content.Server.Shuttles.Systems;
using Content.Server.Shuttles.Components;
using Content.Server.Station.Components;
using Content.Server.Cargo.Systems;
using Content.Server.Station.Systems;
using Content.Shared._Eclipse.SelfShipyard.Components;
using Content.Shared._Eclipse.SelfShipyard;
using Content.Shared.GameTicking;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Shared._Eclipse.SelfShipyard.Events;
using Content.Shared.Mobs.Components;
using Robust.Shared.Containers;
using Content.Shared._NF.Shipyard.Components;
using Content.Shared._Eclipse.CCVar;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using System.Threading.Tasks;
using Content.Server._Eclipse.SelfShipyard.Components;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.ContentPack;
using Content.Server.SelfShipyard.Events;
using Content.Shared.Research.Components;
using Content.Shared.Research.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Map.Components;

namespace Content.Server._Eclipse.SelfShipyard.Systems;

public sealed partial class SelfShipyardSystem : SharedSelfShipyardSystem
{
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly DockingSystem _docking = default!;
    [Dependency] private readonly PricingSystem _pricing = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedResearchSystem _researchSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    [Dependency] private readonly IResourceManager _resMan = default!;

    public MapId? ShipyardMap { get; private set; }
    private float _shuttleIndex;
    private const float ShuttleSpawnBuffer = 1f;
    private ISawmill _sawmill = default!;
    private bool _enabled;
    private float _percentSaveRate;
    private int _constantSaveRate;
    private EntityQuery<MetaDataComponent> _metaQuery;
    private static readonly AfterShuttleDeserializedEvent InitEventInstance = new();

    // The type of error from the attempted sale of a ship.
    public enum ShipyardSaleError
    {
        Success, // Ship can be sold.
        Undocked, // Ship is not docked with the station.
        OrganicsAboard, // Sapient intelligence is aboard, cannot sell, would delete the organics
        InvalidShip, // Ship is invalid
        MessageOverwritten, // Overwritten message.
        InsufficientFunds, //Not enough funds
    }

    // TODO: swap to strictly being a formatted message.
    public struct ShipyardSaleResult
    {
        public ShipyardSaleError Error; // Whether or not the ship can be sold.
        public string? OrganicName; // In case an organic is aboard, this will be set to the first that's aboard.
        public string? OverwrittenMessage; // The message to write if Error is MessageOverwritten.
    }

    public override void Initialize()
    {
        base.Initialize();

        // FIXME: Load-bearing jank - game doesn't want to create a shipyard map at this point.
        _enabled = _configManager.GetCVar(EclipseCCVars.SelfShipyard);
        _configManager.OnValueChanged(EclipseCCVars.SelfShipyard, SetShipyardEnabled); // NOTE: run immediately set to false, see comment above

        _configManager.OnValueChanged(EclipseCCVars.SelfShipyardConstantSaveRate, SetShipyardConstantSaveRate, true);
        _configManager.OnValueChanged(EclipseCCVars.SelfShipyardPercentSaveRate, SetShipyardPercentSaveRate, true);
        _sawmill = Logger.GetSawmill("shipyard");

        _metaQuery = GetEntityQuery<MetaDataComponent>();

        SubscribeLocalEvent<SelfShipyardConsoleComponent, ComponentStartup>(OnShipyardStartup);
        SubscribeLocalEvent<SelfShipyardConsoleComponent, BoundUIOpenedEvent>(OnConsoleUIOpened);
        SubscribeLocalEvent<SelfShipyardConsoleComponent, SelfShipyardConsoleSellMessage>(OnSellMessage);
        SubscribeLocalEvent<SelfShipyardConsoleComponent, SelfShipyardConsolePurchaseMessage>(OnPurchaseMessage);
        SubscribeLocalEvent<SelfShipyardConsoleComponent, EntInsertedIntoContainerMessage>(OnItemSlotChanged);
        SubscribeLocalEvent<SelfShipyardConsoleComponent, EntRemovedFromContainerMessage>(OnItemSlotChanged);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }
    public override void Shutdown()
    {
        _configManager.UnsubValueChanged(EclipseCCVars.SelfShipyard, SetShipyardEnabled);
        _configManager.UnsubValueChanged(EclipseCCVars.SelfShipyardConstantSaveRate, SetShipyardConstantSaveRate);
        _configManager.UnsubValueChanged(EclipseCCVars.SelfShipyardPercentSaveRate, SetShipyardPercentSaveRate);
    }
    private void OnShipyardStartup(EntityUid uid, SelfShipyardConsoleComponent component, ComponentStartup args)
    {
        if (!_enabled)
            return;
        InitializeConsole();
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        CleanupShipyard();
    }

    private void SetShipyardEnabled(bool value)
    {
        if (_enabled == value)
            return;

        _enabled = value;

        if (value)
            SetupShipyardIfNeeded();
        else
            CleanupShipyard();
    }

    private void SetShipyardPercentSaveRate(float value)
    {
        _percentSaveRate = value;
    }

    private void SetShipyardConstantSaveRate(int value)
    {
        _constantSaveRate = value;
    }

    /// <summary>
    /// Adds a ship to the shipyard, calculates its price, and attempts to ftl-dock it to the given station
    /// </summary>
    /// <param name="stationUid">The ID of the station to dock the shuttle to</param>
    /// <param name="shuttlePath">The path to the shuttle file to load. Must be a grid file!</param>
    /// <param name="shuttleEntityUid">The EntityUid of the shuttle that was purchased</param>
    public bool TryPurchaseShuttle(EntityUid stationUid, ResPath shuttlePath, [NotNullWhen(true)] out EntityUid? shuttleEntityUid, out string? dockName)
    {
        dockName = null;
        if (!TryComp<StationDataComponent>(stationUid, out var stationData)
            || !TryAddShuttle(shuttlePath, out var shuttleGrid)
            || !TryComp<ShuttleComponent>(shuttleGrid, out var shuttleComponent))
        {
            shuttleEntityUid = null;
            return false;
        }

        var price = _pricing.AppraiseGrid(shuttleGrid.Value, null);
        var targetGrid = _station.GetLargestGrid(stationData);


        if (targetGrid == null) //how are we even here with no station grid
        {
            QueueDel(shuttleGrid);
            shuttleEntityUid = null;
            return false;
        }

        _sawmill.Info($"Shuttle {shuttlePath} was purchased at {ToPrettyString(stationUid)} for {price:f2}");
        //can do TryFTLDock later instead if we need to keep the shipyard map paused
        if (_shuttle.TryFTLDock(shuttleGrid.Value, shuttleComponent, targetGrid.Value, out var config))
        {
            foreach (var (_, _, dockA, dockB) in config.Docks)
            {
                if (dockB.Name is not null)
                {
                    dockName = Loc.GetString(dockB.Name);
                    break;
                }
            }
        }

        shuttleEntityUid = shuttleGrid;
        return true;
    }

    /// <summary>
    /// Loads a shuttle into the ShipyardMap from a file path
    /// </summary>
    /// <param name="shuttlePath">The path to the grid file to load. Must be a grid file!</param>
    /// <returns>Returns the EntityUid of the shuttle</returns>
    private bool TryAddShuttle(ResPath shuttlePath, [NotNullWhen(true)] out EntityUid? shuttleGrid)
    {
        shuttleGrid = null;
        SetupShipyardIfNeeded();
        if (ShipyardMap == null)
            return false;

        Entity<MapGridComponent>? grid;
        try
        {
            if (!_mapLoader.TryLoadGrid(ShipyardMap.Value, shuttlePath, out grid, offset: new Vector2(500f + _shuttleIndex, 1f)))
            {
                _sawmill.Error($"Unable to spawn shuttle {shuttlePath}");
                return false;
            }
        }
        catch (Exception e)
        {
            _sawmill.Error($"Unable to spawn shuttle {shuttlePath}. Error: {e}");
            return false;
        }

        _shuttleIndex += grid.Value.Comp.LocalAABB.Width + ShuttleSpawnBuffer;

        RecursiveInitEntities(grid.Value.Owner);

        shuttleGrid = grid.Value.Owner;

        return true;
    }

    /// <summary>
    /// Checks a shuttle to make sure that it is docked to the given station, and that there are no lifeforms aboard. Then it teleports tagged items on top of the console, appraises the grid, outputs to the server log, and deletes the grid
    /// </summary>
    /// <param name="stationUid">The ID of the station that the shuttle is docked to</param>
    /// <param name="shuttleUid">The grid ID of the shuttle to be appraised and sold</param>
    /// <param name="consoleUid">The ID of the console being used to sell the ship</param>
    public async Task<(ShipyardSaleResult result, int bill)> TrySaveShuttle(EntityUid player, ICommonSession playerSession, EntityUid stationUid, EntityUid shuttleUid, EntityUid consoleUid, SaveableShuttleComponent saveableShuttle, string shuttleName)
    {
        ShipyardSaleResult result = new ShipyardSaleResult();

        if (!TryComp<StationDataComponent>(stationUid, out var stationGrid)
            || !HasComp<ShuttleComponent>(shuttleUid)
            || !TryComp(shuttleUid, out TransformComponent? xform))
        {
            result.Error = ShipyardSaleError.InvalidShip;
            return (result, 0);
        }

        if (saveableShuttle.PrototypeId == null)
        {
            result.Error = ShipyardSaleError.InvalidShip;
            return (result, 0);
        }

        var targetGrid = _station.GetLargestGrid(stationGrid);

        if (targetGrid == null)
        {
            result.Error = ShipyardSaleError.InvalidShip;
            return (result, 0);
        }

        var gridDocks = _docking.GetDocks(targetGrid.Value);
        var shuttleDocks = _docking.GetDocks(shuttleUid);
        var isDocked = false;

        foreach (var shuttleDock in shuttleDocks)
        {
            foreach (var gridDock in gridDocks)
            {
                if (shuttleDock.Comp.DockedWith == gridDock.Owner)
                {
                    isDocked = true;
                    break;
                }
            }
            if (isDocked)
                break;
        }

        if (!isDocked)
        {
            _sawmill.Warning($"shuttle is not docked to that station");
            result.Error = ShipyardSaleError.Undocked;
            return (result, 0);
        }

        var mobQuery = GetEntityQuery<MobStateComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();

        var charName = FoundOrganics(shuttleUid, mobQuery, xformQuery);
        if (charName is not null)
        {
            _sawmill.Warning($"organics on board");
            result.Error = ShipyardSaleError.OrganicsAboard;
            result.OrganicName = charName;
            return (result, 0);
        }

        //just yeet and delete for now. Might want to split it into another function later to send back to the shipyard map first to pause for something
        //also superman 3 moment
        if (_station.GetOwningStation(shuttleUid) is { Valid: true } shuttleStationUid)
        {
            _station.DeleteStation(shuttleStationUid);
        }

        if (TryComp<SelfShipyardConsoleComponent>(consoleUid, out var comp))
        {
            CleanGrid(shuttleUid, consoleUid);
        }

        var shuttle_cost = (int)_pricing.AppraiseGrid(shuttleUid, PriceCountedCondition);

        var bill = (int)(shuttle_cost * _percentSaveRate) + _constantSaveRate;

        if (!_bank.TryBankWithdraw(player, bill))
        {
            result.Error = ShipyardSaleError.InsufficientFunds;
            return (result, bill);
        }

        _docking.UndockDocks(shuttleUid);

        var id = await _db.AddOwnedShuttle(playerSession.UserId, saveableShuttle.PrototypeId, shuttleName, null, shuttle_cost, ResPath.Root);

        string path = $"/OwnedShuttles/{playerSession.UserId}/{id}.yml";

        var resPath = new ResPath(path);

        _resMan.UserData.CreateDir(resPath.Directory);

        if (!_mapLoader.TrySaveGrid(shuttleUid, resPath))
        {
            await _db.RemoveOwnedShuttle(id, playerSession.UserId);
            _bank.TryBankDeposit(player, bill);
            result.Error = ShipyardSaleError.InvalidShip;
            return (result, bill);
        }
        await _db.UpdateOwnedShuttlePath(id, playerSession.UserId, path);

        QueueDel(shuttleUid);
        _sawmill.Info($"Sold shuttle {shuttleUid} for {bill}");

        // Update all record UI (skip records, no new records)
        _shuttleRecordsSystem.RefreshStateForAll(true);

        result.Error = ShipyardSaleError.Success;
        return (result, bill);
    }

    private void CleanGrid(EntityUid grid, EntityUid destination)
    {
        var xform = Transform(grid);
        var enumerator = xform.ChildEnumerator;
        var entitiesToPreserve = new List<EntityUid>();
        var entitiesToDelete = new List<EntityUid>();

        while (enumerator.MoveNext(out var child))
        {
            FindEntitiesToPreserve(child, ref entitiesToPreserve, ref entitiesToDelete);
        }
        foreach (var ent in entitiesToDelete)
        {
            Del(ent);
        }
        foreach (var ent in entitiesToPreserve)
        {
            // Teleport this item and all its children to the floor (or space).
            _transform.SetCoordinates(ent, new EntityCoordinates(destination, 0, 0));
            _transform.AttachToGridOrMap(ent);
        }
    }

    // checks if something has the ShipyardPreserveOnSaleComponent and if it does, adds it to the list
    private void FindEntitiesToPreserve(EntityUid entity, ref List<EntityUid> toPreserve, ref List<EntityUid> toDelete)
    {
        if (TryComp<ShipyardSellConditionComponent>(entity, out var comp))
        {
            if (comp.PreserveOnSale)
            {
                toPreserve.Add(entity);
                return;
            }
            else if (comp.DeleteOnSale)
            {
                toDelete.Add(entity);
                return;
            }
            else if (comp.ClearSolutionsOnSale)
            {
                if (TryComp<SolutionComponent>(entity, out var soln))
                    _solution.RemoveAllSolution((entity, soln));
            }
        }

        //Reset all researched technologies
        if (TryComp<ResearchServerComponent>(entity, out var researchServer))
        {
            researchServer.Points = 0;
        }
        if (TryComp<TechnologyDatabaseComponent>(entity, out var technologyDatabase))
        {
            _researchSystem.ClearTechs(entity, technologyDatabase);
            technologyDatabase.UnlockedRecipes.Clear();
            technologyDatabase.CurrentTechnologyCards.Clear();
        }

        if (TryComp<ContainerManagerComponent>(entity, out var containers))
        {
            foreach (var container in containers.Containers.Values)
            {
                foreach (var ent in container.ContainedEntities)
                {
                    FindEntitiesToPreserve(ent, ref toPreserve, ref toDelete);
                }
            }
        }
    }

    // returns false if it has ShipyardPreserveOnSaleComponent, true otherwise
    private bool PriceCountedCondition(EntityUid uid)
    {
        return !TryComp<ShipyardSellConditionComponent>(uid, out var comp) || !(comp.PreserveOnSale == true || comp.DeleteOnSale == true);
    }
    private void CleanupShipyard()
    {
        if (ShipyardMap == null || !_map.MapExists(ShipyardMap.Value))
        {
            ShipyardMap = null;
            return;
        }

        _map.DeleteMap(ShipyardMap.Value);
    }

    public void SetupShipyardIfNeeded()
    {
        if (ShipyardMap != null && _map.MapExists(ShipyardMap.Value))
            return;

        _map.CreateMap(out var shipyardMap);
        ShipyardMap = shipyardMap;

        _map.SetPaused(ShipyardMap.Value, false);
    }

    // <summary>
    // Tries to rename a shuttle deed and update the respective components.
    // Returns true if successful.
    //
    // Null name parts are promptly ignored.
    // </summary>
    public bool TryRenameShuttle(EntityUid uid, ShuttleDeedComponent? shuttleDeed, string? newName, string? newSuffix)
    {
        if (!Resolve(uid, ref shuttleDeed))
            return false;

        var shuttle = shuttleDeed.ShuttleUid;
        if (shuttle != null
             && _station.GetOwningStation(shuttle.Value) is { Valid: true } shuttleStation)
        {
            shuttleDeed.ShuttleName = newName;
            shuttleDeed.ShuttleNameSuffix = newSuffix;
            Dirty(uid, shuttleDeed);

            var fullName = GetFullName(shuttleDeed);
            _station.RenameStation(shuttleStation, fullName, loud: false);
            _metaData.SetEntityName(shuttle.Value, fullName);
            _metaData.SetEntityName(shuttleStation, fullName);
        }
        else
        {
            _sawmill.Error($"Could not rename shuttle {ToPrettyString(shuttle):entity} to {newName}");
            return false;
        }

        //TODO: move this to an event that others hook into.
        if (TryGetNetEntity(shuttleDeed.ShuttleUid, out var shuttleNetEntity) &&
            _shuttleRecordsSystem.TryGetRecord(shuttleNetEntity.Value, out var record))
        {
            record.Name = newName ?? "";
            record.Suffix = newSuffix ?? "";
            _shuttleRecordsSystem.TryUpdateRecord(record);
        }

        return true;
    }

    /// <summary>
    /// Returns the full name of the shuttle component in the form of [prefix] [name] [suffix].
    /// </summary>
    public static string GetFullName(ShuttleDeedComponent comp)
    {
        string?[] parts = { comp.ShuttleName, comp.ShuttleNameSuffix };
        return string.Join(' ', parts.Where(it => it != null));
    }

    internal void RecursiveInitEntities(EntityUid entity)
    {
        var toInitialize = new List<EntityUid> { entity };
        for (var i = 0; i < toInitialize.Count; i++)
        {
            var uid = toInitialize[i];
            // toInitialize might contain deleted entities.
            if (!_metaQuery.TryComp(uid, out var meta))
                continue;

            var enumerator = Transform(uid).ChildEnumerator;
            while (enumerator.MoveNext(out var child))
            {
                toInitialize.Add(child);
            }

            EntityManager.EventBus.RaiseLocalEvent(uid, InitEventInstance);
        }
    }
}
