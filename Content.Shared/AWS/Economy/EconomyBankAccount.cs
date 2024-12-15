using Robust.Shared.Serialization;

namespace Content.Shared.AWS.Economy;

[Serializable, NetSerializable]
public sealed class EconomyBankAccount
{
    public string AccountID { get; private set; } = "NO VALUE";

    public string AccountName { get; private set; } = "UNEXPECTED USER";

    public ulong Balance { get; private set; } = 0;

    public ulong Penalty { get; private set; } = 0;

    public bool Blocked { get; private set; } = false;

    public bool CanReachPayDay { get; private set; } = true;

    public List<EconomyBankAccountLogField> Logs { get; private set; } = new();
}