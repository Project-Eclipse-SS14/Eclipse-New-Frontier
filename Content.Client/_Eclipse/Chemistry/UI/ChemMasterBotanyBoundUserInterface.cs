using Content.Client.Chemistry.UI;
using Content.Shared.Chemistry;
using Content.Shared.Containers.ItemSlots;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client._Eclipse.Chemistry.UI
{
    /// <summary>
    /// Initializes a <see cref="ChemMasterBotanyWindow"/> and updates it when new server messages are received.
    /// </summary>
    [UsedImplicitly]
    public sealed class ChemMasterBotanyBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private ChemMasterBotanyWindow? _window;

        public ChemMasterBotanyBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        /// <summary>
        /// Called each time a chem master UI instance is opened. Generates the window and fills it with
        /// relevant info. Sets the actions for static buttons.
        /// </summary>
        protected override void Open()
        {
            base.Open();

            // Setup window layout/elements
            _window = this.CreateWindow<ChemMasterBotanyWindow>();
            _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;

            // Setup static button actions.
            _window.OnEjectBeaker += () => SendMessage(
                new ItemSlotButtonPressedEvent(SharedChemMaster.InputSlotName));
            _window.OutputEjectButton.OnPressed += _ => SendMessage(
                new ItemSlotButtonPressedEvent(SharedChemMaster.OutputSlotName));
            _window.BufferTransferButton.OnPressed += _ => SendMessage(
                new ChemMasterSetModeMessage(ChemMasterMode.Transfer));
            _window.BufferDiscardButton.OnPressed += _ => SendMessage(
                new ChemMasterSetModeMessage(ChemMasterMode.Discard));
            _window.CreatePillButton.OnPressed += _ => SendMessage(
                new ChemMasterCreatePillsMessage(
                    (uint)_window.PillDosage.Value, (uint)_window.PillNumber.Value, _window.LabelLine));
            _window.CreateBottleButton.OnPressed += _ => SendMessage(
                new ChemMasterOutputToBottleMessage(
                    (uint)_window.BottleDosage.Value, _window.LabelLine));
            _window.BufferSortButton.OnPressed += _ => SendMessage(
                    new ChemMasterSortingTypeCycleMessage());

            _window.JuiceButton.OnPressed += _ => SendMessage(
                new ChemMasterStartGrinderMessage());
            _window.OnEjectGrinderChamberAll += () => SendMessage(
                new ChemMasterEjectGrinderChamberAllMessage());
            _window.OnEjectGrinderChamber += uid => SendMessage(
                new ChemMasterEjectGrinderChamberContentMessage(EntMan.GetNetEntity(uid)));

            for (uint i = 0; i < _window.PillTypeButtons.Length; i++)
            {
                var pillType = i;
                _window.PillTypeButtons[i].OnPressed += _ => SendMessage(new ChemMasterSetPillTypeMessage(pillType));
            }

            _window.OnReagentButtonPressed += (args, button) => SendMessage(new ChemMasterReagentAmountButtonMessage(button.Id, button.Amount, button.IsBuffer));
        }

        /// <summary>
        /// Update the ui each time new state data is sent from the server.
        /// </summary>
        /// <param name="state">
        /// Data of the <see cref="SharedReagentDispenserComponent"/> that this ui represents.
        /// Sent from the server.
        /// </param>
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            var castState = (ChemMasterBotanyBoundUserInterfaceState)state;

            _window?.UpdateState(castState); // Update window state
        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            base.ReceiveMessage(message);
            _window?.HandleMessage(message);
        }
    }
}
