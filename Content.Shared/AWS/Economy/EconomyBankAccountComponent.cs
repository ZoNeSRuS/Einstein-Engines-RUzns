using Robust.Shared.Prototypes;
using Content.Shared.Store;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.AWS.Economy
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
    public sealed partial class EconomyBankAccountComponent : Component, IEconomyMoneyHolder
    {
        [ViewVariables(VVAccess.ReadWrite), DataField(required: true)]
        public ProtoId<CurrencyPrototype> AllowCurrency = "Thaler";
        [ViewVariables(VVAccess.ReadWrite), DataField(required: true)]
        public ProtoId<EconomyAccountIdPrototype> AccountIdByProto = "Nanotrasen";
        [ViewVariables(VVAccess.ReadWrite), DataField(required: true)]
        public EntProtoId<EconomyMoneyHolderComponent> MoneyHolderEntId = "ThalerHolder";

        [ViewVariables(VVAccess.ReadWrite), DataField]
        [AutoNetworkedField]
        public ulong Balance { get; set; } = 0;
        [ViewVariables(VVAccess.ReadWrite), DataField]
        [AutoNetworkedField]
        public ulong Penalty = 0;

        [ViewVariables(VVAccess.ReadWrite), DataField]
        [AutoNetworkedField]
        public string AccountId = "NO VALUE";

        [ViewVariables(VVAccess.ReadWrite), DataField]
        [AutoNetworkedField]
        public string AccountName = "UNEXPECTED USER";

        [ViewVariables(VVAccess.ReadWrite), DataField]
        [AutoNetworkedField]
        public bool Blocked = false;

        [ViewVariables(VVAccess.ReadWrite)]
        [AutoNetworkedField]
        public List<EconomyBankAccountLogField> Logs = new();
    }

    [Serializable, NetSerializable]
    public struct EconomyBankAccountLogField
    {
        public EconomyBankAccountLogField(TimeSpan logTime, string logText)
        {
            Date = logTime;
            Text = logText;
        }
        public TimeSpan Date;
        public string Text;
    }
}
