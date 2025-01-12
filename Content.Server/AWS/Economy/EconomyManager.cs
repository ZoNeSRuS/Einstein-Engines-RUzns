using Content.Shared.AWS.Economy;
using Content.Shared.Store;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.AWS.Economy;

public sealed class EconomyManager : IEconomyManager
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public void Initialize()
    {
    }

    public bool TryCreateAccount(string accountID,
                                 string accountName,
                                 ProtoId<CurrencyPrototype> allowedCurrency,
                                 ulong balance = 0,
                                 ulong penalty = 0,
                                 bool blocked = false,
                                 bool canReachPayDay = true,
                                 MapCoordinates? cords = null)
    {
        var sharedSys = _entityManager.System<EconomyBankAccountSystem>();
        return sharedSys.TryCreateAccount(accountID, accountName, allowedCurrency, balance, penalty, blocked, canReachPayDay, cords);
    }

    public bool TryChangeAccountBalance(string accountID, ulong amount, bool addition = true)
    {
        if (!TryGetAccount(accountID, out var entity))
            return false;

        var account = entity.Comp;
        if (!addition)
        {
            if (account.Balance - amount < 0)
                return false;

            account.Balance -= amount;
            return true;
        }

        account.Balance += amount;

        _entityManager.Dirty(entity);
        return true;
    }

    public bool TryTransferMoney(string senderID, string receiverID, ulong amount)
    {
        if (amount <= 0 ||
            !TryGetAccount(senderID, out var senderEntity) ||
            !TryGetAccount(receiverID, out var receiverEntity))
            return false;

        var sender = senderEntity.Comp;
        var receiver = receiverEntity.Comp;
        if (sender.Balance < amount)
            return false;

        sender.Balance -= amount;
        sender.Logs.Add(new(_gameTiming.CurTime, Loc.GetString("economybanksystem-log-send-to",
                    ("amount", amount), ("currencyName", receiver.AllowedCurrency), ("accountId", receiver.AccountID))));
        receiver.Balance += amount;
        receiver.Logs.Add(new(_gameTiming.CurTime, Loc.GetString("economybanksystem-log-send-from",
                    ("amount", amount), ("currencyName", receiver.AllowedCurrency), ("accountId", sender.AccountID))));

        _entityManager.Dirty(senderEntity);
        _entityManager.Dirty(receiverEntity);
        return true;
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

    public void AddLog(string accountID, EconomyBankAccountLogField log)
    {
        if (!TryGetAccount(accountID, out var account))
            return;

        account.Comp.Logs.Add(log);
        _entityManager.Dirty(account);
    }

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
}