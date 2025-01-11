using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.AWS.Economy;
using Content.Shared.Store;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Client.AWS.Economy;

public sealed class ClientEconomyManager : IClientEconomyManager
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public void Initialize()
    {
    }

    public bool IsValidAccount(string accountID) => TryGetAccount(accountID, out _);

    public bool TryGetAccount(string accountID, [NotNullWhen(true)] out Entity<EconomyBankAccountComponent>? account)
    {
        var accounts = GetAccounts(EconomyBankAccountMask.All);
        account = accounts.Where(x => x.Comp.AccountID == accountID).FirstOrDefault();

        return account != null && account.Value.Owner.Id != 0;
    }

    /// <summary>
    /// Returns list of all cached accounts on this client. Use <see cref="AccountUpdateRequest"/> before using for fresh data.
    /// </summary>
    public IReadOnlyList<Entity<EconomyBankAccountComponent>> GetAccounts(EconomyBankAccountMask flag = EconomyBankAccountMask.NotBlocked)
    {
        var accountsEnum = _entityManager.EntityQueryEnumerator<EconomyBankAccountComponent>();
        var list = new List<Entity<EconomyBankAccountComponent>>();

        while (accountsEnum.MoveNext(out var ent, out var comp))
        {
            switch (flag)
            {
                case EconomyBankAccountMask.All:
                    list.Add((ent, comp));
                    break;
                case EconomyBankAccountMask.NotBlocked:
                    if (!comp.Blocked)
                        list.Add((ent, comp));
                    break;
                case EconomyBankAccountMask.Blocked:
                    if (comp.Blocked)
                        list.Add((ent, comp));
                    break;
            }
        }

        return list;
    }

    #region Unused methods (clientside)
    // No client shall be able to create accounts, change balance, transfer money or add logs. EVER.
    public bool TryCreateAccount(string accountID,
                                 string accountName,
                                 ProtoId<CurrencyPrototype> allowedCurrency,
                                 ulong balance = 0,
                                 ulong penalty = 0,
                                 bool blocked = false,
                                 bool canReachPayDay = true)
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