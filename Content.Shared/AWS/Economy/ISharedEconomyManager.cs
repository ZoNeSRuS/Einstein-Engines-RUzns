using Content.Shared.Store;
using Robust.Shared.Map;
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
    bool TryGetAccount(string accountID, out Entity<EconomyBankAccountComponent> account);

    /// <summary>
    /// Returns all currently existing accounts.
    /// </summary>
    /// <param name="flag">Filter mask to fetch accounts.</param>
    IReadOnlyDictionary<string, Entity<EconomyBankAccountComponent>> GetAccounts(EconomyBankAccountMask flag = EconomyBankAccountMask.NotBlocked);

    /// <summary>
    /// Creates a new bank account entity. Really just a redirection to <see cref="EconomyBankAccountSystem"/>. Does not work on client.
    /// </summary>
    /// <param name="accountID"></param>
    /// <param name="accountName"></param>
    /// <param name="allowedCurrency"></param>
    /// <param name="balance"></param>
    /// <param name="penalty"></param>
    /// <param name="blocked"></param>
    /// <param name="canReachPayDay"></param>
    /// <param name="cords"></param>
    /// <returns>Whether the account was successfully created.</returns>
    bool TryCreateAccount(string accountID,
                          string accountName,
                          ProtoId<CurrencyPrototype> allowedCurrency,
                          ulong balance = 0,
                          ulong penalty = 0,
                          bool blocked = false,
                          bool canReachPayDay = true,
                          MapCoordinates? cords = null);

    /// <summary>
    /// Changes the balance of the account.
    /// </summary>
    /// <param name="addition">Whether to add or substract the given amount.</param>
    /// <returns></returns> 
    bool TryChangeAccountBalance(string accountID, ulong amount, bool addition = true);

    /// <summary>
    /// Transfer money from one account to another (with logs).
    /// </summary>
    /// <returns>True if the transfer was successful, false otherwise.</returns>
    bool TryTransferMoney(string senderID, string receiverID, ulong amount);

    /// <summary>
    /// Adds a log to the account.
    /// </summary>
    void AddLog(string accountID, EconomyBankAccountLogField log);

    void Initialize();
}