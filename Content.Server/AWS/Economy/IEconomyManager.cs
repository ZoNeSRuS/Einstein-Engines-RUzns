using Content.Shared.AWS.Economy;

namespace Content.Server.AWS.Economy;

public interface IEconomyManager : ISharedEconomyManager
{
    /// <summary>
    /// Adds account to the account list.
    /// </summary>
    /// <param name="account"></param>
    bool TryAddAccount(EconomyBankAccount account);

    bool TryChangeAccountBalance(string accountID, ulong amount, bool addition = true);

    bool TryTransferMoney(string senderID, string receiverID, ulong amount);

    void AddLog(string accountID, EconomyBankAccountLogField log);
}