namespace Content.Client.AWS.Economy.UI;

public sealed class EconomyLogConsoleBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IClientEconomyManager _economyManager = default!;

    [ViewVariables]
    private EconomyLogConsoleMenu? _menu;

    public EconomyLogConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        // Requset accounts from the server, and when we receive them, open the menu.
        _economyManager.AccountUpdateRequest();
        _economyManager.AccountUpdateReceived += OnAccountUpdate;
    }

    private void OnAccountUpdate(object? sender, EventArgs e)
    {
        _menu = new EconomyLogConsoleMenu(this);
        _menu.OnClose += Close;

        _menu?.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        _economyManager.AccountUpdateReceived -= OnAccountUpdate;
        _menu?.Dispose();
    }
}
