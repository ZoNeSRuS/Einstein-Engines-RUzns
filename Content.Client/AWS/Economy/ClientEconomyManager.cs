using Content.Shared.AWS.Economy;
using Content.Shared.Store;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client.AWS.Economy;

public sealed class ClientEconomyManager : IClientEconomyManager
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public void Initialize()
    {
    }

    public bool IsValidAccount(string accountID)
    {
        var accounts = GetAccounts(EconomyBankAccountMask.All);
        return accounts.ContainsKey(accountID);
    }

    public bool TryGetAccount(string accountID, out Entity<EconomyBankAccountComponent> account)
    {
        var accounts = GetAccounts(EconomyBankAccountMask.All);
        return accounts.TryGetValue(accountID, out account);
    }

    /// <summary>
    /// Returns list of all cached accounts on this client.
    /// </summary>
    public IReadOnlyDictionary<string, Entity<EconomyBankAccountComponent>> GetAccounts(EconomyBankAccountMask flag = EconomyBankAccountMask.NotBlocked)
    {
        var accountsEnum = _entityManager.EntityQueryEnumerator<EconomyBankAccountComponent>();
        var result = new Dictionary<string, Entity<EconomyBankAccountComponent>>();

        while (accountsEnum.MoveNext(out var ent, out var comp))
        {
            switch (flag)
            {
                case EconomyBankAccountMask.All:
                    result.Add(comp.AccountID, (ent, comp));
                    break;
                case EconomyBankAccountMask.NotBlocked:
                    if (!comp.Blocked)
                        result.Add(comp.AccountID, (ent, comp));
                    break;
                case EconomyBankAccountMask.Blocked:
                    if (comp.Blocked)
                        result.Add(comp.AccountID, (ent, comp));
                    break;
            }
        }

        return result;
    }

    #region Unused methods (clientside)
    // No client shall be able to create accounts, change balance, transfer money or add logs. EVER.
    public bool TryCreateAccount(string accountID,
                                 string accountName,
                                 ProtoId<CurrencyPrototype> allowedCurrency,
                                 ulong balance = 0,
                                 ulong penalty = 0,
                                 bool blocked = false,
                                 bool canReachPayDay = true,
                                 MapCoordinates? cords = null)
    {
        return false;
    }

    public bool TryChangeAccountBalance(string accountID, ulong amount, bool addition = true)
    {
        return false;
    }

    public bool TryTransferMoney(string senderID, string receiverID, ulong amount)
    {
        return false;
    }

    public void AddLog(string accountID, EconomyBankAccountLogField log)
    {
        return;
    }
    #endregion
}