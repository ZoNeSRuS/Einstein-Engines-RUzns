using System.Diagnostics.CodeAnalysis;
using Content.Shared.Store;
using Robust.Shared.Prototypes;

namespace Content.Shared.AWS.Economy;

public interface ISharedEconomyManager
{
    /// <summary>
    /// Checks if the account exists (valid).
    /// </summary>
    /// <returns>True if the account exists, false otherwise.</returns>
    bool IsValidAccount(string accountID);

    /// <summary>
    /// Tries to get the account with the given ID.
    /// </summary>
    /// <returns>True if the fetching was successful, false otherwise.</returns>
    bool TryGetAccount(string accountID, [NotNullWhen(true)] out Entity<EconomyBankAccountComponent>? account);

    /// <summary>
    /// Returns all currently existing accounts.
    /// </summary>
    /// <param name="flag">Filter mask to fetch accounts.</param>
    IReadOnlyList<Entity<EconomyBankAccountComponent>> GetAccounts(EconomyBankAccountMask flag = EconomyBankAccountMask.NotBlocked);

    bool TryCreateAccount(string accountID,
                          string accountName,
                          ProtoId<CurrencyPrototype> allowedCurrency,
                          ulong balance = 0,
                          ulong penalty = 0,
                          bool blocked = false,
                          bool canReachPayDay = true);

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

    void Initialize();
}