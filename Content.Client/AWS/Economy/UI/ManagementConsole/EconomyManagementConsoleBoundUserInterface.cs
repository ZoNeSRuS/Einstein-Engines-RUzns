using Content.Shared.AWS.Economy;

namespace Content.Client.AWS.Economy.UI.ManagementConsole;

public sealed class EconomyManagementConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private EconomyManagementConsoleMenu? _menu;

    public EconomyManagementConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _menu = new EconomyManagementConsoleMenu(this);
        _menu.OnClose += Close;

        _menu?.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        _menu?.Dispose();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not EconomyManagementConsoleUserInterfaceState consoleState)
            return;

        _menu?.UpdateState(consoleState);
    }

    public void BlockAccountToggle(EconomyBankAccountComponent? account)
    {
        if (account is null)
            return;

        var blocked = !account.Blocked;
        var msg = new EconomyManagementConsoleChangeParameterMessage(account.AccountID, EconomyBankAccountParam.Blocked, blocked);

        SendMessage(msg);
    }

    public void ChangeName(EconomyBankAccountComponent? account, string newName)
    {
        // reeeee hardcoding
        if (account is null || newName.Length > 40)
            return;

        var msg = new EconomyManagementConsoleChangeParameterMessage(account.AccountID, EconomyBankAccountParam.AccountName, newName);
        SendMessage(msg);
    }

    public void ChangeAccountHolderID(NetEntity holder, string newID)
    {
        var msg = new EconomyManagementConsoleChangeHolderIDMessage(holder, newID);
        SendMessage(msg);
    }

    public void InitializeAccountOnHolder(NetEntity holder)
    {
        var msg = new EconomyManagementConsoleInitAccountOnHolderMessage(holder);
        SendMessage(msg);
    }
}
