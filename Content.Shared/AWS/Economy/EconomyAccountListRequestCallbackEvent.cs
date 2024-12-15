using Robust.Shared.Serialization;

namespace Content.Shared.AWS.Economy;

[Serializable, NetSerializable]
public sealed class EconomyAccountListRequestCallbackEvent : EntityEventArgs
{
    // public Dictionary<string, EconomyBankAccountComponent> Accounts { get; }

    // public EconomyAccountListRequestCallbackEvent(Dictionary<string, EconomyBankAccountComponent> accounts)
    // {
    //     Accounts = accounts;
    // }
}