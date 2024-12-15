using Content.Shared.AWS.Economy;

namespace Content.Server.AWS.Economy;

public sealed class EconomyManager : IEconomyManager
{
    private List<EconomyBankAccount> _accounts = new();

    public void Initialize()
    {

    }

    public void AddAccount(EconomyBankAccount account)
    {
        throw new NotImplementedException();
    }

    public void ChangeAccountBalance(string accountID, ulong amount)
    {
        throw new NotImplementedException();
    }

    public void TransferMoney(string senderID, string receiverID, ulong amount)
    {
        throw new NotImplementedException();
    }

    public Dictionary<string, EconomyBankAccount> GetAccounts(EconomyBankAccountMask flag = EconomyBankAccountMask.Activated)
    {
        throw new NotImplementedException();
    }
}