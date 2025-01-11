using System.Collections.Frozen;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.AWS.Economy;
using Content.Shared.Store;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
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
                                 bool canReachPayDay = true)
    {
        // Check if account already exists.
        if (IsValidAccount(accountID))
            return false;

        var accountEntity = _entityManager.Spawn(null, MapCoordinates.Nullspace);
        var metaData = _entityManager.System<MetaDataSystem>();
        metaData.SetEntityName(accountEntity, accountID);
        var accountComp = _entityManager.EnsureComponent<EconomyBankAccountComponent>(accountEntity);

        accountComp.AccountID = accountID;
        accountComp.AccountName = accountName;
        accountComp.AllowedCurrency = allowedCurrency;
        accountComp.Balance = balance;
        accountComp.Penalty = penalty;
        accountComp.Blocked = blocked;
        accountComp.CanReachPayDay = canReachPayDay;

        _entityManager.Dirty(accountEntity, accountComp);
        return true;
    }

    public bool TryChangeAccountBalance(string accountID, ulong amount, bool addition = true)
    {
        if (!TryGetAccount(accountID, out var entity))
            return false;

        var account = entity.Value.Comp;
        if (!addition)
        {
            if (account.Balance - amount < 0)
                return false;

            account.Balance -= amount;
            return true;
        }

        account.Balance += amount;

        _entityManager.Dirty(entity.Value);
        return true;
    }

    public bool TryTransferMoney(string senderID, string receiverID, ulong amount)
    {
        if (amount <= 0 ||
            !TryGetAccount(senderID, out var senderEntity) ||
            !TryGetAccount(receiverID, out var receiverEntity))
            return false;

        var sender = senderEntity.Value.Comp;
        var receiver = receiverEntity.Value.Comp;
        if (sender.Balance < amount)
            return false;

        sender.Balance -= amount;
        sender.Logs.Add(new(_gameTiming.CurTime, Loc.GetString("economybanksystem-log-send-to",
                    ("amount", amount), ("currencyName", receiver.AllowedCurrency), ("accountId", receiver.AccountID))));
        receiver.Balance += amount;
        receiver.Logs.Add(new(_gameTiming.CurTime, Loc.GetString("economybanksystem-log-send-from",
                    ("amount", amount), ("currencyName", receiver.AllowedCurrency), ("accountId", sender.AccountID))));

        _entityManager.Dirty(senderEntity.Value);
        _entityManager.Dirty(receiverEntity.Value);
        return true;
    }

    public bool IsValidAccount(string accountID) => TryGetAccount(accountID, out _);

    public bool TryGetAccount(string accountID, [NotNullWhen(true)] out Entity<EconomyBankAccountComponent>? account)
    {
        var accounts = GetAccounts(EconomyBankAccountMask.All);
        account = accounts.FirstOrDefault(x => x.Comp.AccountID == accountID);

        return account != null && account.Value.Owner.Id != 0;
    }

    public void AddLog(string accountID, EconomyBankAccountLogField log)
    {
        if (!TryGetAccount(accountID, out var account))
            return;

        account.Value.Comp.Logs.Add(log);
        _entityManager.Dirty(account.Value);
    }

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
}