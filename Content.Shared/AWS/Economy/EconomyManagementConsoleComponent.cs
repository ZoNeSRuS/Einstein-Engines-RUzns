using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.AWS.Economy;

[RegisterComponent]
public sealed partial class EconomyManagementConsoleComponent : Component
{
    public const string ConsoleCardID = "ManagementConsole-IdSlot";

    [DataField("cardSlot")]
    public ItemSlot CardSlot = new();
}

[Serializable, NetSerializable]
public enum EconomyManagementConsoleUiKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class EconomyManagementConsoleChangeParameterMessage(string accountID, EconomyBankAccountParam param, object value) : BoundUserInterfaceMessage
{
    public readonly string AccountID = accountID;
    public readonly EconomyBankAccountParam Param = param;
    public readonly object Value = value;
}

[Serializable, NetSerializable]
public sealed class EconomyManagementConsoleUserInterfaceState : BoundUserInterfaceState
{
    public bool Priveleged;
}
