namespace Content.Shared.AWS.Skills;

public sealed partial class RequiredSkillComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public SkillContainer Container = new();
}
