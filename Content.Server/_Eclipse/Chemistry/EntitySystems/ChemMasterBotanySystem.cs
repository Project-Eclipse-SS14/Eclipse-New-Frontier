using Content.Server._Eclipse.Chemistry.Components;
using Content.Server.Botany.Components;
using Content.Server.Jittering;
using Content.Server.Labels;
using Content.Server.Popups;
using Content.Server.Power.EntitySystems;
using Content.Server.Stack;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.Destructible;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Jittering;
using Content.Shared.Kitchen;
using Content.Shared.Kitchen.Components;
using Content.Shared.Random;
using Content.Shared.Stacks;
using Content.Shared.Storage;
using JetBrains.Annotations;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Server._Eclipse.Chemistry.EntitySystems
{

    /// <summary>
    /// Contains all the server-side logic for ChemMasters.
    /// <seealso cref="ChemMasterBotanyComponent"/>
    /// </summary>
    [UsedImplicitly]
    public sealed class ChemMasterBotanySystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly AudioSystem _audioSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!; // Frontier
        [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly StorageSystem _storageSystem = default!;
        [Dependency] private readonly LabelSystem _labelSystem = default!;
        [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        [Dependency] private readonly RandomHelperSystem _randomHelper = default!;
        [Dependency] private readonly JitteringSystem _jitter = default!;
        [Dependency] private readonly StackSystem _stackSystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly SharedSolutionContainerSystem _solutionContainersSystem = default!;

        [ValidatePrototypeId<EntityPrototype>]
        private const string PillPrototypeId = "Pill";

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ChemMasterBotanyComponent, ComponentStartup>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ChemMasterBotanyComponent, SolutionContainerChangedEvent>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ChemMasterBotanyComponent, EntInsertedIntoContainerMessage>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ChemMasterBotanyComponent, EntRemovedFromContainerMessage>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ChemMasterBotanyComponent, ContainerIsRemovingAttemptEvent>(OnEntRemoveAttempt);
            SubscribeLocalEvent<ChemMasterBotanyComponent, BoundUIOpenedEvent>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ChemMasterBotanyComponent, InteractUsingEvent>(OnInteractUsing);

            SubscribeLocalEvent<ChemMasterBotanyComponent, ChemMasterSetModeMessage>(OnSetModeMessage);
            SubscribeLocalEvent<ChemMasterBotanyComponent, ChemMasterSortingTypeCycleMessage>(OnCycleSortingTypeMessage);
            SubscribeLocalEvent<ChemMasterBotanyComponent, ChemMasterSetPillTypeMessage>(OnSetPillTypeMessage);
            SubscribeLocalEvent<ChemMasterBotanyComponent, ChemMasterReagentAmountButtonMessage>(OnReagentButtonMessage);
            SubscribeLocalEvent<ChemMasterBotanyComponent, ChemMasterCreatePillsMessage>(OnCreatePillsMessage);
            SubscribeLocalEvent<ChemMasterBotanyComponent, ChemMasterOutputToBottleMessage>(OnOutputToBottleMessage);
            SubscribeLocalEvent<ChemMasterBotanyComponent, ChemMasterStartGrinderMessage>(OnStartGrinderMessage);
            SubscribeLocalEvent<ChemMasterBotanyComponent, ChemMasterEjectGrinderChamberAllMessage>(OnEjectGrinderChamberAllMessage);
            SubscribeLocalEvent<ChemMasterBotanyComponent, ChemMasterEjectGrinderChamberContentMessage>(OnEjectGrinderChamberContentMessage);

            SubscribeLocalEvent<ActiveChemMasterGrinderComponent, ComponentStartup>(OnActiveGrinderStart);
            SubscribeLocalEvent<ActiveChemMasterGrinderComponent, ComponentRemove>(OnActiveGrinderRemove);
        }

        private void OnInteractUsing(Entity<ChemMasterBotanyComponent> entity, ref InteractUsingEvent args)
        {
            var heldEnt = args.Used;
            var inputContainer = _containerSystem.EnsureContainer<Container>(entity.Owner, SharedReagentGrinder.InputContainerId);

            if (!HasComp<ExtractableComponent>(heldEnt) || !HasComp<ProduceComponent>(heldEnt))
            {
                if (!HasComp<FitsInDispenserComponent>(heldEnt))
                {
                    // This is ugly but we can't use whitelistFailPopup because there are 2 containers with different whitelists.
                    _popupSystem.PopupEntity(Loc.GetString("chem-master-component-cannot-put-entity-message"), entity.Owner, args.User);
                }

                // Entity did NOT pass the whitelist for grind/juice.
                // Wouldn't want the clown grinding up the Captain's ID card now would you?
                // Why am I asking you? You're biased.
                return;
            }

            if (args.Handled)
                return;

            // Cap the chamber. Don't want someone putting in 500 entities and ejecting them all at once.
            // Maybe I should have done that for the microwave too?
            if (inputContainer.ContainedEntities.Count >= entity.Comp.StorageMaxEntities)
                return;

            if (!_containerSystem.Insert(heldEnt, inputContainer))
                return;

            args.Handled = true;
        }

        private void OnEjectGrinderChamberAllMessage(Entity<ChemMasterBotanyComponent> entity, ref ChemMasterEjectGrinderChamberAllMessage message)
        {
            var inputContainer = _containerSystem.EnsureContainer<Container>(entity.Owner, SharedReagentGrinder.InputContainerId);

            if (HasComp<ActiveChemMasterGrinderComponent>(entity) || inputContainer.ContainedEntities.Count <= 0)
                return;

            ClickSound(entity);
            foreach (var toEject in inputContainer.ContainedEntities.ToList())
            {
                _containerSystem.Remove(toEject, inputContainer);
                _randomHelper.RandomOffset(toEject, 0.4f);
            }
            UpdateUiState(entity);
        }

        private void OnEjectGrinderChamberContentMessage(Entity<ChemMasterBotanyComponent> entity, ref ChemMasterEjectGrinderChamberContentMessage message)
        {
            if (HasComp<ActiveChemMasterGrinderComponent>(entity))
                return;

            var inputContainer = _containerSystem.EnsureContainer<Container>(entity.Owner, SharedReagentGrinder.InputContainerId);
            var ent = GetEntity(message.EntityId);

            if (_containerSystem.Remove(ent, inputContainer))
            {
                _randomHelper.RandomOffset(ent, 0.4f);
                ClickSound(entity);
                UpdateUiState(entity);
            }
        }

        private void OnStartGrinderMessage(Entity<ChemMasterBotanyComponent> entity, ref ChemMasterStartGrinderMessage message)
        {
            if (!this.IsPowered(entity.Owner, EntityManager) || HasComp<ActiveChemMasterGrinderComponent>(entity))
                return;

            DoGrinding(entity.Owner, entity.Comp);
        }

        private void OnActiveGrinderStart(Entity<ActiveChemMasterGrinderComponent> ent, ref ComponentStartup args)
        {
            _jitter.AddJitter(ent, -10, 100);
        }

        private void OnActiveGrinderRemove(Entity<ActiveChemMasterGrinderComponent> ent, ref ComponentRemove args)
        {
            RemComp<JitteringComponent>(ent);
        }

        private void SubscribeUpdateUiState<T>(Entity<ChemMasterBotanyComponent> ent, ref T ev)
        {
            UpdateUiState(ent);
        }

        private void OnEntRemoveAttempt(Entity<ChemMasterBotanyComponent> entity, ref ContainerIsRemovingAttemptEvent args)
        {
            if (HasComp<ActiveChemMasterGrinderComponent>(entity))
                args.Cancel();
        }

        private void UpdateUiState(Entity<ChemMasterBotanyComponent> ent, bool updateLabel = false)
        {
            var (owner, chemMaster) = ent;
            if (!_solutionContainerSystem.TryGetSolution(owner, SharedChemMaster.BufferSolutionName, out _, out var bufferSolution))
                return;
            var inputContainer = _itemSlotsSystem.GetItemOrNull(owner, SharedChemMaster.InputSlotName);
            _appearanceSystem.SetData(owner, ChemMasterVisualState.BeakerInserted, inputContainer.HasValue); // Frontier
            var outputContainer = _itemSlotsSystem.GetItemOrNull(owner, SharedChemMaster.OutputSlotName);
            var inputGrinderContainer = _containerSystem.EnsureContainer<Container>(owner, SharedReagentGrinder.InputContainerId);

            var bufferReagents = bufferSolution.Contents;
            var bufferCurrentVolume = bufferSolution.Volume;

            var state = new ChemMasterBotanyBoundUserInterfaceState(
                chemMaster.Mode, chemMaster.SortingType, BuildInputContainerInfo(inputContainer), BuildOutputContainerInfo(outputContainer),
                bufferReagents, bufferCurrentVolume, chemMaster.PillType, chemMaster.PillDosageLimit, updateLabel, GetNetEntityArray(inputGrinderContainer.ContainedEntities.ToArray()));

            _userInterfaceSystem.SetUiState(owner, ChemMasterBotanyUiKey.Key, state);
        }

        private void OnSetModeMessage(Entity<ChemMasterBotanyComponent> chemMaster, ref ChemMasterSetModeMessage message)
        {
            // Ensure the mode is valid, either Transfer or Discard.
            if (!Enum.IsDefined(typeof(ChemMasterMode), message.ChemMasterMode))
                return;

            chemMaster.Comp.Mode = message.ChemMasterMode;
            UpdateUiState(chemMaster);
            ClickSound(chemMaster);
        }

        private void OnCycleSortingTypeMessage(Entity<ChemMasterBotanyComponent> chemMaster, ref ChemMasterSortingTypeCycleMessage message)
        {
            chemMaster.Comp.SortingType++;
            if (chemMaster.Comp.SortingType > ChemMasterSortingType.Latest)
                chemMaster.Comp.SortingType = ChemMasterSortingType.None;
            UpdateUiState(chemMaster);
            ClickSound(chemMaster);
        }

        private void OnSetPillTypeMessage(Entity<ChemMasterBotanyComponent> chemMaster, ref ChemMasterSetPillTypeMessage message)
        {
            // Ensure valid pill type. There are 20 pills selectable, 0-19.
            if (message.PillType > SharedChemMaster.PillTypes - 1)
                return;

            chemMaster.Comp.PillType = message.PillType;
            UpdateUiState(chemMaster);
            ClickSound(chemMaster);
        }

        private void OnReagentButtonMessage(Entity<ChemMasterBotanyComponent> chemMaster, ref ChemMasterReagentAmountButtonMessage message)
        {
            // Ensure the amount corresponds to one of the reagent amount buttons.
            if (!Enum.IsDefined(typeof(ChemMasterReagentAmount), message.Amount))
                return;

            switch (chemMaster.Comp.Mode)
            {
                case ChemMasterMode.Transfer:
                    TransferReagents(chemMaster, message.ReagentId, message.Amount.GetFixedPoint(), message.FromBuffer);
                    break;
                case ChemMasterMode.Discard:
                    DiscardReagents(chemMaster, message.ReagentId, message.Amount.GetFixedPoint(), message.FromBuffer);
                    break;
                default:
                    // Invalid mode.
                    return;
            }

            ClickSound(chemMaster);
        }

        private void TransferReagents(Entity<ChemMasterBotanyComponent> chemMaster, ReagentId id, FixedPoint2 amount, bool fromBuffer)
        {
            if (HasComp<ActiveChemMasterGrinderComponent>(chemMaster))
                return;
            var container = _itemSlotsSystem.GetItemOrNull(chemMaster, SharedChemMaster.InputSlotName);
            if (container is null ||
                !_solutionContainerSystem.TryGetFitsInDispenser(container.Value, out var containerSoln, out var containerSolution) ||
                !_solutionContainerSystem.TryGetSolution(chemMaster.Owner, SharedChemMaster.BufferSolutionName, out _, out var bufferSolution))
            {
                return;
            }

            if (fromBuffer) // Buffer to container
            {
                amount = FixedPoint2.Min(amount, containerSolution.AvailableVolume);
                amount = bufferSolution.RemoveReagent(id, amount, preserveOrder: true);
                _solutionContainerSystem.TryAddReagent(containerSoln.Value, id, amount, out var _);
            }
            else // Container to buffer
            {
                amount = FixedPoint2.Min(amount, containerSolution.GetReagentQuantity(id));
                _solutionContainerSystem.RemoveReagent(containerSoln.Value, id, amount);
                bufferSolution.AddReagent(id, amount);
            }

            UpdateUiState(chemMaster, updateLabel: true);
        }

        private void DiscardReagents(Entity<ChemMasterBotanyComponent> chemMaster, ReagentId id, FixedPoint2 amount, bool fromBuffer)
        {
            if (fromBuffer)
            {
                if (HasComp<ActiveChemMasterGrinderComponent>(chemMaster))
                    return;
                if (_solutionContainerSystem.TryGetSolution(chemMaster.Owner, SharedChemMaster.BufferSolutionName, out _, out var bufferSolution))
                    bufferSolution.RemoveReagent(id, amount, preserveOrder: true);
                else
                    return;
            }
            else
            {
                var container = _itemSlotsSystem.GetItemOrNull(chemMaster, SharedChemMaster.InputSlotName);
                if (container is not null &&
                    _solutionContainerSystem.TryGetFitsInDispenser(container.Value, out var containerSolution, out _))
                {
                    _solutionContainerSystem.RemoveReagent(containerSolution.Value, id, amount);
                }
                else
                    return;
            }

            UpdateUiState(chemMaster, updateLabel: fromBuffer);
        }

        private void OnCreatePillsMessage(Entity<ChemMasterBotanyComponent> chemMaster, ref ChemMasterCreatePillsMessage message)
        {
            var user = message.Actor;
            var maybeContainer = _itemSlotsSystem.GetItemOrNull(chemMaster, SharedChemMaster.OutputSlotName);
            if (maybeContainer is not { Valid: true } container
                || !TryComp(container, out StorageComponent? storage))
            {
                return; // output can't fit pills
            }

            // Ensure the number is valid.
            if (message.Number == 0 || !_storageSystem.HasSpace((container, storage)))
                return;

            // Ensure the amount is valid.
            if (message.Dosage == 0 || message.Dosage > chemMaster.Comp.PillDosageLimit)
                return;

            // Ensure label length is within the character limit.
            if (message.Label.Length > SharedChemMaster.LabelMaxLength)
                return;

            var needed = message.Dosage * message.Number;
            if (!WithdrawFromBuffer(chemMaster, needed, user, out var withdrawal))
                return;

            _labelSystem.Label(container, message.Label);

            for (var i = 0; i < message.Number; i++)
            {
                var item = Spawn(PillPrototypeId, Transform(container).Coordinates);
                _storageSystem.Insert(container, item, out _, user: user, storage);
                _labelSystem.Label(item, message.Label);

                _solutionContainerSystem.EnsureSolutionEntity(item, SharedChemMaster.PillSolutionName, out var itemSolution, message.Dosage);
                if (!itemSolution.HasValue)
                    return;

                _solutionContainerSystem.TryAddSolution(itemSolution.Value, withdrawal.SplitSolution(message.Dosage));

                var pill = EnsureComp<PillComponent>(item);
                pill.PillType = chemMaster.Comp.PillType;
                Dirty(item, pill);

                // Log pill creation by a user
                _adminLogger.Add(LogType.Action, LogImpact.Low,
                    $"{ToPrettyString(user):user} printed {ToPrettyString(item):pill} {SharedSolutionContainerSystem.ToPrettyString(itemSolution.Value.Comp.Solution)}");
            }

            UpdateUiState(chemMaster);
            ClickSound(chemMaster);
        }

        private void OnOutputToBottleMessage(Entity<ChemMasterBotanyComponent> chemMaster, ref ChemMasterOutputToBottleMessage message)
        {
            var user = message.Actor;
            var maybeContainer = _itemSlotsSystem.GetItemOrNull(chemMaster, SharedChemMaster.OutputSlotName);
            if (maybeContainer is not { Valid: true } container
                || !_solutionContainerSystem.TryGetSolution(container, SharedChemMaster.BottleSolutionName, out var soln, out var solution))
            {
                return; // output can't fit reagents
            }

            // Ensure the amount is valid.
            if (message.Dosage == 0 || message.Dosage > solution.AvailableVolume)
                return;

            // Ensure label length is within the character limit.
            if (message.Label.Length > SharedChemMaster.LabelMaxLength)
                return;

            if (!WithdrawFromBuffer(chemMaster, message.Dosage, user, out var withdrawal))
                return;

            _labelSystem.Label(container, message.Label);
            _solutionContainerSystem.TryAddSolution(soln.Value, withdrawal);

            // Log bottle creation by a user
            _adminLogger.Add(LogType.Action, LogImpact.Low,
                $"{ToPrettyString(user):user} bottled {ToPrettyString(container):bottle} {SharedSolutionContainerSystem.ToPrettyString(solution)}");

            UpdateUiState(chemMaster);
            ClickSound(chemMaster);
        }

        private bool WithdrawFromBuffer(
            Entity<ChemMasterBotanyComponent> chemMaster,
            FixedPoint2 neededVolume, EntityUid? user,
            [NotNullWhen(returnValue: true)] out Solution? outputSolution)
        {
            outputSolution = null;

            if (HasComp<ActiveChemMasterGrinderComponent>(chemMaster))
                return false;

            if (!_solutionContainerSystem.TryGetSolution(chemMaster.Owner, SharedChemMaster.BufferSolutionName, out _, out var solution))
            {
                return false;
            }

            if (solution.Volume == 0)
            {
                if (user.HasValue)
                    _popupSystem.PopupCursor(Loc.GetString("chem-master-window-buffer-empty-text"), user.Value);
                return false;
            }

            // ReSharper disable once InvertIf
            if (neededVolume > solution.Volume)
            {
                if (user.HasValue)
                    _popupSystem.PopupCursor(Loc.GetString("chem-master-window-buffer-low-text"), user.Value);
                return false;
            }

            outputSolution = solution.SplitSolution(neededVolume);
            return true;
        }

        private void ClickSound(Entity<ChemMasterBotanyComponent> chemMaster)
        {
            _audioSystem.PlayPvs(chemMaster.Comp.ClickSound, chemMaster, AudioParams.Default.WithVolume(-2f));
        }

        private ContainerInfo? BuildInputContainerInfo(EntityUid? container)
        {
            if (container is not { Valid: true })
                return null;

            if (!TryComp(container, out FitsInDispenserComponent? fits)
                || !_solutionContainerSystem.TryGetSolution(container.Value, fits.Solution, out _, out var solution))
            {
                return null;
            }

            return BuildContainerInfo(Name(container.Value), solution);
        }

        private ContainerInfo? BuildOutputContainerInfo(EntityUid? container)
        {
            if (container is not { Valid: true })
                return null;

            var name = Name(container.Value);
            {
                if (_solutionContainerSystem.TryGetSolution(
                        container.Value, SharedChemMaster.BottleSolutionName, out _, out var solution))
                {
                    return BuildContainerInfo(name, solution);
                }
            }

            if (!TryComp(container, out StorageComponent? storage))
                return null;

            var pills = storage.Container.ContainedEntities.Select((Func<EntityUid, (string, FixedPoint2 quantity)>)(pill =>
            {
                _solutionContainerSystem.TryGetSolution(pill, SharedChemMaster.PillSolutionName, out _, out var solution);
                var quantity = solution?.Volume ?? FixedPoint2.Zero;
                return (Name(pill), quantity);
            })).ToList();

            return new ContainerInfo(name, _storageSystem.GetCumulativeItemAreas((container.Value, storage)), storage.Grid.GetArea())
            {
                Entities = pills
            };
        }

        private static ContainerInfo BuildContainerInfo(string name, Solution solution)
        {
            return new ContainerInfo(name, solution.Volume, solution.MaxVolume)
            {
                Reagents = solution.Contents
            };
        }

        private void DoGrinding(EntityUid uid, ChemMasterBotanyComponent component)
        {
            var inputContainer = _containerSystem.EnsureContainer<Container>(uid, SharedReagentGrinder.InputContainerId);

            // Do we have anything to grind/juice and a container to put the reagents in?
            if (inputContainer.ContainedEntities.Count <= 0)
                return;

            var sound = component.JuiceSound;
            var program = GrinderProgram.Grind;

            var active = AddComp<ActiveChemMasterGrinderComponent>(uid);
            active.EndTime = _timing.CurTime + component.WorkTime * component.WorkTimeMultiplier;

            component.AudioStream = _audioSystem.PlayPvs(sound, uid,
                AudioParams.Default.WithPitchScale(1 / component.WorkTimeMultiplier))?.Entity; //slightly higher pitched
            _userInterfaceSystem.ServerSendUiMessage(uid, ChemMasterBotanyUiKey.Key,
                new ReagentGrinderWorkStartedMessage(program));
        }

        private Solution? GetGrindSolution(EntityUid uid)
        {
            if (TryComp<ExtractableComponent>(uid, out var extractable)
                && extractable.GrindableSolution is not null
                && _solutionContainersSystem.TryGetSolution(uid, extractable.GrindableSolution, out _, out var solution))
            {
                return solution;
            }
            else
                return null;
        }
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<ActiveChemMasterGrinderComponent, ChemMasterBotanyComponent>();
            while (query.MoveNext(out var uid, out var active, out var reagentGrinder))
            {
                if (active.EndTime > _timing.CurTime)
                    continue;

                reagentGrinder.AudioStream = _audioSystem.Stop(reagentGrinder.AudioStream);
                RemCompDeferred<ActiveChemMasterGrinderComponent>(uid);

                var inputContainer = _containerSystem.EnsureContainer<Container>(uid, SharedReagentGrinder.InputContainerId);
                if (!_solutionContainerSystem.TryGetSolution(uid, SharedChemMaster.BufferSolutionName, out _, out var bufferSolution))
                    return;

                foreach (var item in inputContainer.ContainedEntities.ToList())
                {
                    var solution = GetGrindSolution(item);

                    if (solution is null)
                        continue;

                    if (TryComp<StackComponent>(item, out var stack))
                    {
                        var totalVolume = solution.Volume * stack.Count;
                        if (totalVolume <= 0)
                            continue;

                        // Buffer is assumed to be infinite
                        // Maximum number of items we can process in the stack without going over AvailableVolume
                        // We add a small tolerance, because floats are inaccurate.
                        //var fitsCount = (int) (stack.Count * FixedPoint2.Min(containerSolution.AvailableVolume / totalVolume + 0.01, 1));
                        var fitsCount = stack.Count;
                        if (fitsCount <= 0)
                            continue;

                        // Make a copy of the solution to scale
                        // Otherwise we'll actually change the volume of the remaining stack too
                        var scaledSolution = new Solution(solution);
                        scaledSolution.ScaleSolution(fitsCount);
                        solution = scaledSolution;

                        _stackSystem.SetCount(item, stack.Count - fitsCount); // Setting to 0 will QueueDel
                    }
                    else
                    {
                        // Buffer is assumed to be infinite
                        //if (solution.Volume > containerSolution.AvailableVolume)
                        //    continue;

                        var dev = new DestructionEventArgs();
                        RaiseLocalEvent(item, dev);

                        QueueDel(item);
                    }

                    bufferSolution.AddSolution(solution, _prototypeManager);
                }

                _userInterfaceSystem.ServerSendUiMessage(uid, ChemMasterBotanyUiKey.Key,
                    new ReagentGrinderWorkCompleteMessage());

                UpdateUiState((uid, reagentGrinder));
            }
        }
    }
}
