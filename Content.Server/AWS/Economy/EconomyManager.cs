using System.Collections.Frozen;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.AWS.Economy;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.AWS.Economy;

public sealed class EconomyManager : IEconomyManager
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IServerNetManager _netManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private Dictionary<string, EconomyBankAccount> _accounts = new();

    public void Initialize()
    {
        _netManager.RegisterNetMessage<MsgEconomyAccountList>();

        _netManager.RegisterNetMessage<MsgEconomyAccountListRequest>(AccountListUpdateRequest);

        _playerManager.PlayerStatusChanged += OnPlayerJoinedGame;
    }

    public bool TryAddAccount(EconomyBankAccount account)
    {
        // Maybe do other checks here as well?
        // If account already exists, return false.
        if (IsValidAccount(account.AccountID))
            return false;

        return _accounts.TryAdd(account.AccountID, account);
    }

    public bool TryChangeAccountBalance(string accountID, ulong amount, bool addition = true)
    {
        if (!TryGetAccount(accountID, out var account))
            return false;

        if (!addition)
        {
            if (account.Balance - amount < 0)
                return false;

            account.Balance -= amount;
            return true;
        }

        account.Balance += amount;
        return true;
    }

    public bool TryTransferMoney(string senderID, string receiverID, ulong amount)
    {
        if (amount <= 0 ||
            !TryGetAccount(senderID, out var sender) ||
            !TryGetAccount(receiverID, out var receiver))
            return false;

        if (sender.Balance < amount)
            return false;

        sender.Balance -= amount;
        sender.Logs.Add(new(_gameTiming.CurTime, Loc.GetString("economybanksystem-log-send-to",
                    ("amount", amount), ("currencyName", receiver.AllowedCurrency), ("accountId", receiver.AccountID))));
        receiver.Balance += amount;
        receiver.Logs.Add(new(_gameTiming.CurTime, Loc.GetString("economybanksystem-log-send-from",
                    ("amount", amount), ("currencyName", receiver.AllowedCurrency), ("accountId", sender.AccountID))));
        return true;
    }

    public bool IsValidAccount(string accountID)
    {
        return _accounts.ContainsKey(accountID);
    }

    public bool TryGetAccount(string accountID, [NotNullWhen(true)] out EconomyBankAccount? account)
    {
        return _accounts.TryGetValue(accountID, out account);
    }

    public void AddLog(string accountID, EconomyBankAccountLogField log)
    {
        if (!TryGetAccount(accountID, out var account))
            return;

        account.Logs.Add(log);
    }

    public IReadOnlyDictionary<string, EconomyBankAccount> GetAccounts(EconomyBankAccountMask flag = EconomyBankAccountMask.NotBlocked)
    {
        if (flag == EconomyBankAccountMask.All)
        {
            ReadOnlyDictionary<string, EconomyBankAccount> all = new(_accounts);
            return all;
        }

        Dictionary<string, EconomyBankAccount> list = new();
        var accountsEnum = _accounts.GetEnumerator();
        while (accountsEnum.MoveNext())
        {
            var account = accountsEnum.Current.Value;
            switch (flag)
            {
                case EconomyBankAccountMask.NotBlocked:
                    if (!account.Blocked)
                        list.Add(account.AccountID, account);
                    break;
                case EconomyBankAccountMask.Blocked:
                    if (account.Blocked)
                        list.Add(account.AccountID, account);
                    break;
            }
        }

        ReadOnlyDictionary<string, EconomyBankAccount> result = new(list);
        return result;
    }

    private void UpdateAccountList(INetChannel channel)
    {
        var msg = new MsgEconomyAccountList();
        msg.Accounts = _accounts.ToFrozenDictionary();
        _netManager.ServerSendMessage(msg, channel);
    }

    private void AccountListUpdateRequest(MsgEconomyAccountListRequest message)
    {
        UpdateAccountList(message.MsgChannel);
    }

    private void OnPlayerJoinedGame(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus != SessionStatus.InGame)
            return;

        UpdateAccountList(e.Session.Channel);
    }
}