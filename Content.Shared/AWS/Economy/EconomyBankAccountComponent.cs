using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Content.Shared.Store;

namespace Content.Shared.AWS.Economy
{
    /// <summary>
    /// This component is used to define the account. This component should not be created manually. Work with it through <cref>ISharedEconomyManager</cref>.
    /// </summary>
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class EconomyBankAccountComponent : Component
    {
        /// <summary>
        /// Set this up in <cref>EconomyBankAccountSetup</cref> to define the account, which this card will be using (referring to).
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [AutoNetworkedField]
        public string AccountID = "NO VALUE";

        /// <summary>
        /// Set this up in <cref>EconomyBankAccountSetup</cref> to define the name, which this card will be using.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [AutoNetworkedField]
        public string AccountName = "UNEXPECTED USER";

        [ViewVariables(VVAccess.ReadWrite)]
        [AutoNetworkedField]
        public ProtoId<CurrencyPrototype> AllowedCurrency = "Thaler";

        [ViewVariables(VVAccess.ReadWrite)]
        [AutoNetworkedField]
        public ulong Balance = 0;

        [ViewVariables(VVAccess.ReadWrite)]
        [AutoNetworkedField]
        public ulong Penalty = 0;

        [ViewVariables(VVAccess.ReadWrite)]
        [AutoNetworkedField]
        public bool Blocked;

        [ViewVariables(VVAccess.ReadWrite)]
        [AutoNetworkedField]
        public bool CanReachPayDay = true;

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
