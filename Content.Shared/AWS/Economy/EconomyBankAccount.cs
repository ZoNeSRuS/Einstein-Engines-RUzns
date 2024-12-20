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

    public EconomyBankAccount(string accountID, string accountName = "UNEXPECTED USER",
                              string allowedCurrency = "Thaler",
                              ulong balance = 0, ulong penalty = 0,
                              bool blocked = false, bool canReachPayday = true,
                              List<EconomyBankAccountLogField>? logs = default)
    {
        AccountID = accountID;
        AccountName = accountName;
        AllowedCurrency = allowedCurrency;
        Balance = balance;
        Penalty = penalty;
        Blocked = blocked;
        CanReachPayDay = canReachPayday;
        Logs = logs ?? [];
    }
}