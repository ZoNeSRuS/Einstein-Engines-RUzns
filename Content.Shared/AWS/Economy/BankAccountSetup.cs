using Content.Shared.Store;
using Robust.Shared.Prototypes;

namespace Content.Shared.AWS.Economy;

[DataDefinition]
public sealed partial class BankAccountSetup
{
    [DataField("accountID", required: true)]
    public string AccountID = "NO VALUE";

    [DataField("accountName")]
    public string AccountName = "UNEXPECTED USER";

    [DataField("allowedCurrency")]
    public ProtoId<CurrencyPrototype> AllowedCurrency = "Thaler";

    [DataField("balance")]
    public ulong Balance = 0;

    [DataField("penalty")]
    public ulong Penalty = 0;

    [DataField("blocked")]
    public bool Blocked = false;

    [DataField("canReachPayDay")]
    public bool CanReachPayDay = true;
}