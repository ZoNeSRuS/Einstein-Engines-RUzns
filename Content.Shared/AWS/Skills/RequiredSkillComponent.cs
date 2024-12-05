using Robust.Shared.GameStates;

namespace Content.Shared.AWS.Skills;

[RegisterComponent, NetworkedComponent]
public sealed partial class RequiredSkillComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public SkillContainer Container = new();
}
