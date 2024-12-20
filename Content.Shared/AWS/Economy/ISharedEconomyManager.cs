using System.Diagnostics.CodeAnalysis;

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
    bool TryGetAccount(string accountID, [NotNullWhen(true)] out EconomyBankAccount? account);

    /// <summary>
    /// Returns all currently existing accounts.
    /// </summary>
    /// <param name="flag">Filter mask to fetch accounts.</param>
    IReadOnlyDictionary<string, EconomyBankAccount> GetAccounts(EconomyBankAccountMask flag = EconomyBankAccountMask.NotBlocked);

    void Initialize();
}