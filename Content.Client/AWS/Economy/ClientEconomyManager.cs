using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.AWS.Economy;
using Robust.Shared.Network;

namespace Content.Client.AWS.Economy;

public sealed class ClientEconomyManager : IClientEconomyManager
{
    [Dependency] private readonly IClientNetManager _netManager = default!;

    /// <summary>
    /// List of all cached accounts on this client.
    /// </summary>
    private FrozenDictionary<string, EconomyBankAccount> _cachedAccounts = default!;

    public void Initialize()
    {
        _netManager.RegisterNetMessage<MsgEconomyAccountListRequest>();

        _netManager.RegisterNetMessage<MsgEconomyAccountList>(AccountsUpdateMessage);
    }

    public bool IsValidAccount(string accountID)
    {
        AccountUpdateRequest();
        return _cachedAccounts.ContainsKey(accountID);
    }

    public bool TryGetAccount(string accountID, [NotNullWhen(true)] out EconomyBankAccount? account)
    {
        AccountUpdateRequest();
        return _cachedAccounts.TryGetValue(accountID, out account);
    }

    /// <summary>
    /// Returns list of all cached accounts on this client. Use <see cref="AccountUpdateRequest"/> before using for fresh data.
    /// </summary>
    public IReadOnlyDictionary<string, EconomyBankAccount> GetAccounts(EconomyBankAccountMask flag = EconomyBankAccountMask.NotBlocked)
    {
        if (flag == EconomyBankAccountMask.All)
            return _cachedAccounts;

        Dictionary<string, EconomyBankAccount> list = new();
        var accountsEnum = _cachedAccounts.GetEnumerator();
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

        return list;
    }

    public void AccountUpdateRequest()
    {
        var msg = new MsgEconomyAccountListRequest();
        _netManager.ClientSendMessage(msg);
    }

    private void AccountsUpdateMessage(MsgEconomyAccountList message)
    {
        _cachedAccounts = message.Accounts.ToFrozenDictionary();
    }
}