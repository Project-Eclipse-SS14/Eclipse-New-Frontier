using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Construction; // Frontier
using Content.Server.Fluids.EntitySystems;
using Content.Server.Lathe.Components;
using Content.Server.Materials;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Radio.EntitySystems;
using Content.Server.Stack;
using Content.Shared.Atmos;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.UserInterface;
using Content.Shared.Database;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.Lathe;
using Content.Shared.Lathe.Prototypes;
using Content.Shared.Localizations;
using Content.Shared.Materials;
using Content.Shared.Power;
using Content.Shared.ReagentSpeed;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using JetBrains.Annotations;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared.Cargo.Components; // Frontier
using Content.Server._NF.Contraband.Systems; // Frontier
using Robust.Shared.Containers;
using Content.Shared._NF.Lathe; // Frontier

namespace Content.Server.Lathe
{
    [UsedImplicitly]
    public sealed class LatheSystem : SharedLatheSystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IPrototypeManager _proto = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly ContainerSystem _container = default!;
        [Dependency] private readonly EmagSystem _emag = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSys = default!;
        [Dependency] private readonly MaterialStorageSystem _materialStorage = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly PuddleSystem _puddle = default!;
        [Dependency] private readonly ReagentSpeedSystem _reagentSpeed = default!;
        [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
        [Dependency] private readonly StackSystem _stack = default!;
        [Dependency] private readonly TransformSystem _transform = default!;
        [Dependency] private readonly RadioSystem _radio = default!;
        [Dependency] private readonly ContrabandTurnInSystem _contraband = default!; // Frontier

        /// <summary>
        /// Per-tick cache
        /// </summary>
        private readonly List<GasMixture> _environments = new();
        private const int MaxItemsPerRequest = 100_000; // Frontier

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<LatheComponent, GetMaterialWhitelistEvent>(OnGetWhitelist);
            SubscribeLocalEvent<LatheComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<LatheComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<LatheComponent, TechnologyDatabaseModifiedEvent>(OnDatabaseModified);
            SubscribeLocalEvent<LatheAnnouncingComponent, TechnologyDatabaseModifiedEvent>(OnTechnologyDatabaseModified);
            SubscribeLocalEvent<LatheComponent, ResearchRegistrationChangedEvent>(OnResearchRegistrationChanged);

            SubscribeLocalEvent<LatheComponent, LatheQueueRecipeMessage>(OnLatheQueueRecipeMessage);
            SubscribeLocalEvent<LatheComponent, LatheSyncRequestMessage>(OnLatheSyncRequestMessage);
            SubscribeLocalEvent<LatheComponent, LatheDeleteRequestMessage>(OnLatheDeleteRequestMessage); // Frontier
            SubscribeLocalEvent<LatheComponent, LatheMoveRequestMessage>(OnLatheMoveRequestMessage); // Frontier
            SubscribeLocalEvent<LatheComponent, LatheAbortFabricationMessage>(OnLatheAbortFabricationMessage); // Frontier

            SubscribeLocalEvent<LatheComponent, BeforeActivatableUIOpenEvent>((u, c, _) => UpdateUserInterfaceState(u, c));
            SubscribeLocalEvent<LatheComponent, MaterialAmountChangedEvent>(OnMaterialAmountChanged);
            SubscribeLocalEvent<TechnologyDatabaseComponent, LatheGetRecipesEvent>(OnGetRecipes);
            SubscribeLocalEvent<EmagLatheRecipesComponent, LatheGetRecipesEvent>(GetEmagLatheRecipes);
            SubscribeLocalEvent<LatheHeatProducingComponent, LatheStartPrintingEvent>(OnHeatStartPrinting);

            //Frontier: upgradeable parts
            SubscribeLocalEvent<LatheComponent, RefreshPartsEvent>(OnPartsRefresh);
            SubscribeLocalEvent<LatheComponent, UpgradeExamineEvent>(OnUpgradeExamine);
        }
        public override void Update(float frameTime)
        {
            var query = EntityQueryEnumerator<LatheProducingComponent, LatheComponent>();
            while (query.MoveNext(out var uid, out var comp, out var lathe))
            {
                if (lathe.CurrentRecipe == null)
                    continue;

                if (_timing.CurTime - comp.StartTime >= (comp.ProductionLength * 3))
                    FinishProducing(uid, lathe);
            }

            var heatQuery = EntityQueryEnumerator<LatheHeatProducingComponent, LatheProducingComponent, TransformComponent>();
            while (heatQuery.MoveNext(out var uid, out var heatComp, out _, out var xform))
            {
                if (_timing.CurTime < heatComp.NextSecond)
                    continue;
                heatComp.NextSecond += TimeSpan.FromSeconds(1);

                var position = _transform.GetGridTilePositionOrDefault((uid, xform));
                _environments.Clear();

                if (_atmosphere.GetTileMixture(xform.GridUid, xform.MapUid, position, true) is { } tileMix)
                    _environments.Add(tileMix);

                if (xform.GridUid != null)
                {
                    var enumerator = _atmosphere.GetAdjacentTileMixtures(xform.GridUid.Value, position, false, true);
                    while (enumerator.MoveNext(out var mix))
                    {
                        _environments.Add(mix);
                    }
                }

                if (_environments.Count > 0)
                {
                    var heatPerTile = heatComp.EnergyPerSecond / _environments.Count;
                    foreach (var env in _environments)
                    {
                        _atmosphere.AddHeat(env, heatPerTile);
                    }
                }
            }
        }

        private void OnGetWhitelist(EntityUid uid, LatheComponent component, ref GetMaterialWhitelistEvent args)
        {
            if (args.Storage != uid)
                return;
            var materialWhitelist = new List<ProtoId<MaterialPrototype>>();
            var recipes = GetAvailableRecipes(uid, component, true);
            foreach (var id in recipes)
            {
                if (!_proto.TryIndex(id, out var proto))
                    continue;
                foreach (var (mat, _) in proto.Materials)
                {
                    if (!materialWhitelist.Contains(mat))
                    {
                        materialWhitelist.Add(mat);
                    }
                }
            }

            var combined = args.Whitelist.Union(materialWhitelist).ToList();
            args.Whitelist = combined;
        }

        [PublicAPI]
        public bool TryGetAvailableRecipes(EntityUid uid, [NotNullWhen(true)] out List<ProtoId<LatheRecipePrototype>>? recipes, [NotNullWhen(true)] LatheComponent? component = null, bool getUnavailable = false)
        {
            recipes = null;
            if (!Resolve(uid, ref component))
                return false;
            recipes = GetAvailableRecipes(uid, component, getUnavailable);
            return true;
        }

        public List<ProtoId<LatheRecipePrototype>> GetAvailableRecipes(EntityUid uid, LatheComponent component, bool getUnavailable = false)
        {
            var ev = new LatheGetRecipesEvent((uid, component), getUnavailable);
            AddRecipesFromPacks(ev.Recipes, component.StaticPacks);
            RaiseLocalEvent(uid, ev);
            return ev.Recipes.ToList();
        }

        public bool TryAddToQueue(EntityUid uid, LatheRecipePrototype recipe, int quantity, LatheComponent? component = null) // Frontier: add quantity
        {
            if (!Resolve(uid, ref component))
                return false;

            // Frontier: argument check
            if (quantity <= 0)
                return false;
            quantity = int.Min(quantity, MaxItemsPerRequest);
            // Frontier: argument check

            if (!CanProduce(uid, recipe, quantity, component)) // Frontier: 1<quantity
                return false;

            foreach (var (mat, amount) in recipe.Materials)
            {
                var adjustedAmount = recipe.ApplyMaterialDiscount
                    ? (int)(-amount * component.FinalMaterialUseMultiplier) // Frontier: MaterialUseMultiplier<FinalMaterialUseMultiplier
                    : -amount;
                adjustedAmount *= quantity; // Frontier

                _materialStorage.TryChangeMaterialAmount(uid, mat, adjustedAmount);
            }

            // Frontier: queue up a batch
            if (component.Queue.Count > 0 && component.Queue[^1].Recipe.ID == recipe.ID)
                component.Queue[^1].ItemsRequested += quantity;
            else
                component.Queue.Add(new LatheRecipeBatch(recipe, 0, quantity));
            // End Frontier
            // component.Queue.Add(recipe); // Frontier

            return true;
        }

        public bool TryStartProducing(EntityUid uid, LatheComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return false;
            if (component.CurrentRecipe != null || component.Queue.Count <= 0 || !this.IsPowered(uid, EntityManager))
                return false;

            // Frontier: handle batches
            var batch = component.Queue.First();
            batch.ItemsPrinted++;
            if (batch.ItemsPrinted >= batch.ItemsRequested || batch.ItemsPrinted < 0) // Rollover sanity check
                component.Queue.RemoveAt(0);
            var recipe = batch.Recipe;
            // End Frontier

            var time = _reagentSpeed.ApplySpeed(uid, recipe.CompleteTime) * component.TimeMultiplier;

            var lathe = EnsureComp<LatheProducingComponent>(uid);
            lathe.StartTime = _timing.CurTime;
            lathe.ProductionLength = time * component.FinalTimeMultiplier; // Frontier: TimeMultiplier<FinalTimeMultiplier
            component.CurrentRecipe = recipe;

            var ev = new LatheStartPrintingEvent(recipe);
            RaiseLocalEvent(uid, ref ev);

            _audio.PlayPvs(component.ProducingSound, uid);
            UpdateRunningAppearance(uid, true);
            UpdateUserInterfaceState(uid, component);

            if (time == TimeSpan.Zero)
            {
                FinishProducing(uid, component, lathe);
            }
            return true;
        }

        public void FinishProducing(EntityUid uid, LatheComponent? comp = null, LatheProducingComponent? prodComp = null)
        {
            if (!Resolve(uid, ref comp, ref prodComp, false))
                return;

            if (comp.CurrentRecipe != null)
            {
                if (comp.CurrentRecipe.Result is { } resultProto)
                {
                    var result = Spawn(resultProto, Transform(uid).Coordinates);

                    // Frontier: adjust price before merge (stack prices changed once)
                    if (result.Valid)
                    {
                        ModifyPrintedEntityPrice(uid, comp, result);

                        _contraband.ClearContrabandValue(result);
                    }
                    // End Frontier

                    _stack.TryMergeToContacts(result);
                }

                if (comp.CurrentRecipe.ResultReagents is { } resultReagents &&
                    comp.ReagentOutputSlotId is { } slotId)
                {
                    var toAdd = new Solution(
                        resultReagents.Select(p => new ReagentQuantity(p.Key.Id, p.Value, null)));

                    // dispense it in the container if we have it and dump it if we don't
                    if (_container.TryGetContainer(uid, slotId, out var container) &&
                        container.ContainedEntities.Count == 1 &&
                        _solution.TryGetFitsInDispenser(container.ContainedEntities.First(), out var solution, out _))
                    {
                        _solution.AddSolution(solution.Value, toAdd);
                    }
                    else
                    {
                        _popup.PopupEntity(Loc.GetString("lathe-reagent-dispense-no-container", ("name", uid)), uid);
                        _puddle.TrySpillAt(uid, toAdd, out _);
                    }
                }
            }

            comp.CurrentRecipe = null;
            prodComp.StartTime = _timing.CurTime;

            if (!TryStartProducing(uid, comp))
            {
                RemCompDeferred(uid, prodComp);
                UpdateUserInterfaceState(uid, comp);
                UpdateRunningAppearance(uid, false);
            }
        }

        public void UpdateUserInterfaceState(EntityUid uid, LatheComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            var producing = component.CurrentRecipe ?? component.Queue.FirstOrDefault()?.Recipe; // Frontier: add ?.Recipe

            var state = new LatheUpdateState(GetAvailableRecipes(uid, component), component.Queue, producing);
            _uiSys.SetUiState(uid, LatheUiKey.Key, state);
        }

        /// <summary>
        /// Adds every unlocked recipe from each pack to the recipes list.
        /// </summary>
        public void AddRecipesFromDynamicPacks(ref LatheGetRecipesEvent args, TechnologyDatabaseComponent database, IEnumerable<ProtoId<LatheRecipePackPrototype>> packs)
        {
            foreach (var id in packs)
            {
                var pack = _proto.Index(id);
                foreach (var recipe in pack.Recipes)
                {
                    if (args.GetUnavailable || database.UnlockedRecipes.Contains(recipe))
                        args.Recipes.Add(recipe);
                }
            }
        }

        private void OnGetRecipes(EntityUid uid, TechnologyDatabaseComponent component, LatheGetRecipesEvent args)
        {
            if (uid == args.Lathe)
                AddRecipesFromDynamicPacks(ref args, component, args.Comp.DynamicPacks);
        }

        private void GetEmagLatheRecipes(EntityUid uid, EmagLatheRecipesComponent component, LatheGetRecipesEvent args)
        {
            if (uid != args.Lathe)
                return;

            if (!args.GetUnavailable && !_emag.CheckFlag(uid, EmagType.Interaction))
                return;

            AddRecipesFromPacks(args.Recipes, component.EmagStaticPacks);

            if (TryComp<TechnologyDatabaseComponent>(uid, out var database))
                AddRecipesFromDynamicPacks(ref args, database, component.EmagDynamicPacks);
        }

        private void OnHeatStartPrinting(EntityUid uid, LatheHeatProducingComponent component, LatheStartPrintingEvent args)
        {
            component.NextSecond = _timing.CurTime;
        }

        private void OnMaterialAmountChanged(EntityUid uid, LatheComponent component, ref MaterialAmountChangedEvent args)
        {
            UpdateUserInterfaceState(uid, component);
        }

        /// <summary>
        /// Initialize the UI and appearance.
        /// Appearance requires initialization or the layers break
        /// </summary>
        private void OnMapInit(EntityUid uid, LatheComponent component, MapInitEvent args)
        {
            _appearance.SetData(uid, LatheVisuals.IsInserting, false);
            _appearance.SetData(uid, LatheVisuals.IsRunning, false);

            _materialStorage.UpdateMaterialWhitelist(uid);
            // New Frontiers - Lathe Upgrades - initialization of upgrade coefficients
            // This code is licensed under AGPLv3. See AGPLv3.txt
            component.FinalTimeMultiplier = component.TimeMultiplier;
            component.FinalMaterialUseMultiplier = component.MaterialUseMultiplier;
            // End of modified code
        }

        /// <summary>
        /// Sets the machine sprite to either play the running animation
        /// or stop.
        /// </summary>
        private void UpdateRunningAppearance(EntityUid uid, bool isRunning)
        {
            _appearance.SetData(uid, LatheVisuals.IsRunning, isRunning);
        }

        private void OnPowerChanged(EntityUid uid, LatheComponent component, ref PowerChangedEvent args)
        {
            if (!args.Powered)
            {
                AbortProduction(uid); // Frontier
                // RemComp<LatheProducingComponent>(uid); // Frontier
                // UpdateRunningAppearance(uid, false); // Frontier
            }
            else /*if (component.CurrentRecipe != null)*/ // Frontier
            {
                //EnsureComp<LatheProducingComponent>(uid); // Frontier
                TryStartProducing(uid, component);
            }
        }

        private void OnDatabaseModified(EntityUid uid, LatheComponent component, ref TechnologyDatabaseModifiedEvent args)
        {
            UpdateUserInterfaceState(uid, component);
        }

        private void OnTechnologyDatabaseModified(Entity<LatheAnnouncingComponent> ent, ref TechnologyDatabaseModifiedEvent args)
        {
            if (args.NewlyUnlockedRecipes is null)
                return;

            if (!TryGetAvailableRecipes(ent.Owner, out var potentialRecipes))
                return;

            var recipeNames = new List<string>();
            foreach (var recipeId in args.NewlyUnlockedRecipes)
            {
                if (!potentialRecipes.Contains(new(recipeId)))
                    continue;

                if (!_proto.TryIndex(recipeId, out LatheRecipePrototype? recipe))
                    continue;

                var itemName = GetRecipeName(recipe!);
                recipeNames.Add(Loc.GetString("lathe-unlock-recipe-radio-broadcast-item", ("item", itemName)));
            }

            if (recipeNames.Count == 0)
                return;

            var message =
                recipeNames.Count > ent.Comp.MaximumItems ?
                    Loc.GetString(
                        "lathe-unlock-recipe-radio-broadcast-overflow",
                        ("items", ContentLocalizationManager.FormatList(recipeNames.GetRange(0, ent.Comp.MaximumItems))),
                        ("count", recipeNames.Count)
                    ) :
                    Loc.GetString(
                        "lathe-unlock-recipe-radio-broadcast",
                        ("items", ContentLocalizationManager.FormatList(recipeNames))
                    );

            foreach (var channel in ent.Comp.Channels)
            {
                _radio.SendRadioMessage(ent.Owner, message, channel, ent.Owner, escapeMarkup: false);
            }
        }

        private void OnResearchRegistrationChanged(EntityUid uid, LatheComponent component, ref ResearchRegistrationChangedEvent args)
        {
            UpdateUserInterfaceState(uid, component);
        }

        protected override bool HasRecipe(EntityUid uid, LatheRecipePrototype recipe, LatheComponent component)
        {
            return GetAvailableRecipes(uid, component).Contains(recipe.ID);
        }

        #region UI Messages

        private void OnLatheQueueRecipeMessage(EntityUid uid, LatheComponent component, LatheQueueRecipeMessage args)
        {
            if (_proto.TryIndex(args.ID, out LatheRecipePrototype? recipe))
            {
                // Frontier: batching recipes
                if (TryAddToQueue(uid, recipe, args.Quantity, component))
                {
                    _adminLogger.Add(LogType.Action,
                        LogImpact.Low,
                        $"{ToPrettyString(args.Actor):player} queued {args.Quantity} {GetRecipeName(recipe)} at {ToPrettyString(uid):lathe}");
                }
                // End Frontier
            }
            TryStartProducing(uid, component);
            UpdateUserInterfaceState(uid, component);
        }

        private void OnLatheSyncRequestMessage(EntityUid uid, LatheComponent component, LatheSyncRequestMessage args)
        {
            UpdateUserInterfaceState(uid, component);
        }
        #endregion


        // New Frontiers - Lathe Upgrades - upgrading lathe speed through machine parts
        // This code is licensed under AGPLv3. See AGPLv3.txt
        private void OnPartsRefresh(EntityUid uid, LatheComponent component, RefreshPartsEvent args)
        {
            var printTimeRating = args.PartRatings[component.MachinePartPrintSpeed];
            var materialUseRating = args.PartRatings[component.MachinePartMaterialUse];

            component.FinalTimeMultiplier = component.TimeMultiplier * MathF.Pow(component.PartRatingPrintTimeMultiplier, printTimeRating - 1);
            component.FinalMaterialUseMultiplier = component.MaterialUseMultiplier * MathF.Pow(component.PartRatingMaterialUseMultiplier, materialUseRating - 1);
            Dirty(uid, component);
        }

        private void OnUpgradeExamine(EntityUid uid, LatheComponent component, UpgradeExamineEvent args)
        {
            args.AddPercentageUpgrade("lathe-component-upgrade-speed", 1 / component.FinalTimeMultiplier);
            args.AddPercentageUpgrade("lathe-component-upgrade-material-use", component.FinalMaterialUseMultiplier);
        }

        // Frontier: modify item value, remove from queue
        #region Frontier
        private void ModifyPrintedEntityPrice(EntityUid uid, LatheComponent component, EntityUid target)
        {
            // Cannot reduce value, leave item as-is
            if (component.ProductValueModifier == null
            || !float.IsFinite(component.ProductValueModifier.Value)
            || component.ProductValueModifier < 0f)
                return;

            if (TryComp<StackPriceComponent>(target, out var stackPrice))
            {
                if (stackPrice.Price > 0)
                    stackPrice.Price *= component.ProductValueModifier.Value;
            }
            if (TryComp<StaticPriceComponent>(target, out var staticPrice))
            {
                if (staticPrice.Price > 0)
                    staticPrice.Price *= component.ProductValueModifier.Value;
            }

            // Recurse into contained entities
            if (TryComp<ContainerManagerComponent>(target, out var containers))
            {
                foreach (var container in containers.Containers.Values)
                {
                    foreach (var ent in container.ContainedEntities)
                    {
                        ModifyPrintedEntityPrice(uid, component, ent);
                    }
                }
            }
        }

        public void AbortProduction(EntityUid uid, LatheComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;
            if (component.CurrentRecipe != null)
            {
                // Items incremented on start, need to decrement with removal
                if (component.Queue.Count > 0)
                {
                    var batch = component.Queue.First();
                    if (batch.Recipe != component.CurrentRecipe)
                    {
                        var newBatch = new LatheRecipeBatch(component.CurrentRecipe, 0, 1);
                        component.Queue.Insert(0, newBatch);
                    }
                    else if (batch.ItemsPrinted > 0)
                    {
                        batch.ItemsPrinted--;
                    }
                }

                component.CurrentRecipe = null;
            }
            RemCompDeferred<LatheProducingComponent>(uid);
            UpdateUserInterfaceState(uid, component);
            UpdateRunningAppearance(uid, false);
        }

        public void OnLatheDeleteRequestMessage(EntityUid uid, LatheComponent component, ref LatheDeleteRequestMessage args)
        {
            if (args.Index < 0 || args.Index >= component.Queue.Count)
                return;

            var batch = component.Queue[args.Index];
            _adminLogger.Add(LogType.Action,
                LogImpact.Low,
                $"{ToPrettyString(args.Actor):player} deleted a lathe job for ({batch.ItemsPrinted}/{batch.ItemsRequested}) {GetRecipeName(batch.Recipe)} at {ToPrettyString(uid):lathe}");

            component.Queue.RemoveAt(args.Index);

            // Eclipse-Start : Return materials on recipe cancel
            var quantity = batch.ItemsRequested - batch.ItemsPrinted;
            foreach (var (mat, amount) in batch.Recipe.Materials)
            {
                var adjustedAmount = batch.Recipe.ApplyMaterialDiscount
                    ? (int)(amount * component.FinalMaterialUseMultiplier) // Frontier: MaterialUseMultiplier<FinalMaterialUseMultiplier
                    : amount;
                adjustedAmount *= quantity; // Frontier

                _materialStorage.TryChangeMaterialAmount(uid, mat, adjustedAmount);
            }
            // Eclipse-End

            UpdateUserInterfaceState(uid, component);
        }

        public void OnLatheMoveRequestMessage(EntityUid uid, LatheComponent component, ref LatheMoveRequestMessage args)
        {
            if (args.Change == 0 || args.Index < 0 || args.Index >= component.Queue.Count)
                return;

            var newIndex = args.Index + args.Change;
            if (newIndex < 0 || newIndex >= component.Queue.Count)
                return;

            var temp = component.Queue[args.Index];
            component.Queue[args.Index] = component.Queue[newIndex];
            component.Queue[newIndex] = temp;
            UpdateUserInterfaceState(uid, component);
        }

        public void OnLatheAbortFabricationMessage(EntityUid uid, LatheComponent component, ref LatheAbortFabricationMessage args)
        {
            if (component.CurrentRecipe == null)
                return;

            _adminLogger.Add(LogType.Action,
                LogImpact.Low,
                $"{ToPrettyString(args.Actor):player} aborted printing {GetRecipeName(component.CurrentRecipe)} at {ToPrettyString(uid):lathe}");

            component.CurrentRecipe = null;
            FinishProducing(uid, component);
        }
        #endregion
        // End Frontier
    }
}
