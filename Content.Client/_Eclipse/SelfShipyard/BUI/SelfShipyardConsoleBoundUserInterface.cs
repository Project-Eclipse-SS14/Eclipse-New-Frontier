using Content.Client._Eclipse.SelfShipyard.UI;
using Content.Shared.Containers.ItemSlots;
using Content.Shared._Eclipse.SelfShipyard.BUI;
using Content.Shared._Eclipse.SelfShipyard.Events;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client._Eclipse.SelfShipyard.BUI;

public sealed class SelfShipyardConsoleBoundUserInterface : BoundUserInterface
{
    private SelfShipyardConsoleMenu? _menu;
    public int Balance { get; private set; }

    public int? ShipSellValue { get; private set; }

    public SelfShipyardConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _menu = new SelfShipyardConsoleMenu(this);
        // Disable the NFSD popup for now.
        // var rules = new FormattedMessage();
        // _rulesWindow = new ShipyardRulesPopup(this);
        _menu.OpenCentered();
        // if (ShipyardConsoleUiKey.Security == (ShipyardConsoleUiKey) UiKey)
        // {
        //     rules.AddText(Loc.GetString($"shipyard-rules-default1"));
        //     rules.PushNewline();
        //     rules.AddText(Loc.GetString($"shipyard-rules-default2"));
        //     _rulesWindow.ShipRules.SetMessage(rules);
        //     _rulesWindow.OpenCentered();
        // }
        _menu.OnClose += Close;
        _menu.OnOrderApproved += ApproveOrder;
        _menu.OnSellShip += SellShip;
        _menu.TargetIdButton.OnPressed += _ => SendMessage(new ItemSlotButtonPressedEvent("SelfShipyardConsole-targetId"));
    }

    private void Populate(List<OwnedVesselVisibleRecord> availablePrototypes, bool validId)
    {
        if (_menu == null)
            return;

        _menu.PopulateProducts(availablePrototypes, validId);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not SelfShipyardConsoleInterfaceState cState)
            return;

        Balance = cState.Balance;
        ShipSellValue = cState.ShipSaveRate;
        var castState = (SelfShipyardConsoleInterfaceState)state;
        Populate(castState.ShipyardPrototypes, castState.IsTargetIdPresent);
        _menu?.UpdateState(castState);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing) return;

        _menu?.Dispose();
    }

    private void ApproveOrder(ButtonEventArgs args)
    {
        if (args.Button.Parent?.Parent is not OwnedVesselRow row || row.Vessel == null)
        {
            return;
        }

        var vesselId = row.Vessel.Id;
        SendMessage(new SelfShipyardConsolePurchaseMessage(vesselId));
    }
    private void SellShip(ButtonEventArgs args)
    {
        //reserved for a sanity check, but im not sure what since we check all the important stuffs on server already
        SendMessage(new SelfShipyardConsoleSellMessage());
    }
}
