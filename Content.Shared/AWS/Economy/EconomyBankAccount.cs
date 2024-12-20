using Content.Shared.Store;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.AWS.Economy;

[Serializable, NetSerializable]
public sealed class EconomyBankAccount
{
    public string AccountID { get; set; }

    public string AccountName { get; set; }

    public ProtoId<CurrencyPrototype> AllowedCurrency { get; set; }

    public ulong Balance { get; set; }

    public ulong Penalty { get; set; }

    public bool Blocked { get; set; }

    public bool CanReachPayDay { get; set; }

    public List<EconomyBankAccountLogField> Logs { get; set; }

    public EconomyBankAccount(string accountID, string? accountName,
                              string? allowedCurrency,
                              ulong? balance, ulong? penalty,
                              bool? blocked, bool? canReachPayday,
                              List<EconomyBankAccountLogField>? logs = default)
    {
        AccountID = accountID;
        AccountName = accountName ?? "UNEXPECTED USER";
        AllowedCurrency = allowedCurrency ?? "Thaler";
        Balance = balance ?? 0;
        Penalty = penalty ?? 0;
        Blocked = blocked ?? false;
        CanReachPayDay = canReachPayday ?? true;
        Logs = logs ?? [];
    }
}