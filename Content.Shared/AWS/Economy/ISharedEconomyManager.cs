using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.AWS.Economy;

public interface ISharedEconomyManager
{
    bool IsValidAccount(string accountID);

    bool TryGetAccount(string accountID, [NotNullWhen(true)] out EconomyBankAccount? account);

    /// <summary>
    /// Returns all currently existing accounts.
    /// </summary>
    /// <param name="flag">Filter mask to fetch accounts.</param>
    IReadOnlyDictionary<string, EconomyBankAccount> GetAccounts(EconomyBankAccountMask flag = EconomyBankAccountMask.NotBlocked);

    void Initialize();
}