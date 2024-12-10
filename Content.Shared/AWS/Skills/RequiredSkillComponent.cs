using Robust.Shared.GameStates;

namespace Content.Shared.AWS.Skills;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RequiredSkillComponent : Component
{
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite), DataField(required: true)]
    public SkillContainer Container = new();
}
