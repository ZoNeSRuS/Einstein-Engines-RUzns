using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.AWS.Economy;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EconomyManagementConsoleComponent : Component
{
    public const string ConsoleCardID = "ManagementConsole-IdSlot";
    public const string TargetCardID = "ManagementConsole-Target-IdSlot";

    [DataField("cardSlot"), AutoNetworkedField]
    public ItemSlot CardSlot = new();

    [DataField("targetCardSlot"), AutoNetworkedField]
    public ItemSlot TargetCardSlot = new();
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
public sealed class EconomyManagementConsoleChangeHolderIDMessage(NetEntity holder, string newID) : BoundUserInterfaceMessage
{
    public readonly NetEntity AccountHolder = holder;
    public readonly string NewID = newID;
}

[Serializable, NetSerializable]
public sealed class EconomyManagementConsoleInitAccountOnHolderMessage(NetEntity holder) : BoundUserInterfaceMessage
{
    public readonly NetEntity AccountHolder = holder;
}

[Serializable, NetSerializable]
public sealed class EconomyManagementConsoleUserInterfaceState : BoundUserInterfaceState
{
    public bool Priveleged;
    public NetEntity? Holder;
}
