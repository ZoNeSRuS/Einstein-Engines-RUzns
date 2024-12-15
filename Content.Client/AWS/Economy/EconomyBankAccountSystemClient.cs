using Content.Shared.AWS.Economy;

namespace Content.Client.AWS.Economy;

public sealed class EconomyBankAccountSystemClient : EconomyBankAccountSystemShared
{
    public Dictionary<string, EconomyBankAccountComponent> CachedAccounts { get; private set; } = new();

    public override void Initialize()
    {
        SubscribeNetworkEvent<EconomyAccountListRequestCallbackEvent>(OnAccountRequestCallback);
    }

    public void RequestBankAccounts()
    {
        var args = new EconomyAccountListRequestEvent();
        RaiseNetworkEvent(args);
    }

    private void OnAccountRequestCallback(EconomyAccountListRequestCallbackEvent args)
    {
        //CachedAccounts = args.Accounts;
    }
}