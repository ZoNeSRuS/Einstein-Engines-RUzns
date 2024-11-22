using Robust.Shared.Prototypes;

namespace Content.Shared.AWS.Skills;

[Serializable]
public sealed class SkillContainer
{
    [ViewVariables(VVAccess.ReadWrite), DataField(required: true)]
    public Dictionary<ProtoId<SkillPrototype>, SkillLevel> Skills = new();

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public Dictionary<ProtoId<SkillPrototype>, List<SkillLevel>> UnblockedSkillLevels = new();

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public uint AdditionalSkillPoints = 0;
}
