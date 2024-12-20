using Content.Shared.AWS.Economy;

namespace Content.Server.AWS.Economy;

public interface IEconomyManager : ISharedEconomyManager
{
    /// <summary>
    /// Adds account to the account list.
    /// </summary>
    /// <returns>True if the account was successfully added, false otherwise.</returns>
    bool TryAddAccount(EconomyBankAccount account);

    /// <summary>
    /// Changes the balance of the account.
    /// </summary>
    /// <param name="addition">Whether to add or substract the given amount.</param>
    /// <returns></returns> 
    bool TryChangeAccountBalance(string accountID, ulong amount, bool addition = true);

    /// <summary>
    /// Transfer money from one account to another.
    /// </summary>
    /// <returns>True if the transfer was successful, false otherwise.</returns>
    bool TryTransferMoney(string senderID, string receiverID, ulong amount);

    /// <summary>
    /// Adds a log to the account.
    /// </summary>
    void AddLog(string accountID, EconomyBankAccountLogField log);
}