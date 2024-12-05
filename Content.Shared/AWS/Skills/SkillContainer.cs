using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.AWS.Skills;

[Serializable, NetSerializable]
public sealed class SkillContainer
{
    [ViewVariables(VVAccess.ReadWrite), DataField(required: true)]
    public Dictionary<ProtoId<SkillPrototype>, Enum> Skills = new();                        // Enum = SkillLevel

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public Dictionary<ProtoId<SkillPrototype>, List<Enum>> UnblockedSkillLevels = new();    // Enum = SkillLevel

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public uint AdditionalSkillPoints = 0;
}
