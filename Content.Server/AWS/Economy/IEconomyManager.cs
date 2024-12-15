using Content.Shared.AWS.Economy;

namespace Content.Server.AWS.Economy;

public interface IEconomyManager
{
    /// <summary>
    /// Adds account to the account list.
    /// </summary>
    /// <param name="account"></param>
    void AddAccount(EconomyBankAccount account);

    void ChangeAccountBalance(string accountID, ulong amount);

    void TransferMoney(string senderID, string receiverID, ulong amount);

    /// <summary>
    /// Returns all currently existing accounts.
    /// </summary>
    /// <param name="flag">Filter mask to fetch accounts.</param>
    Dictionary<string, EconomyBankAccount> GetAccounts(EconomyBankAccountMask flag = EconomyBankAccountMask.Activated);

    void Initialize();
}