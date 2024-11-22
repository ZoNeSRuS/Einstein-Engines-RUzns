using Robust.Shared.Prototypes;

namespace Content.Shared.AWS.Skills;

[Prototype("skill")]
public sealed partial class SkillPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = string.Empty;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public Dictionary<SkillLevel, uint> Cost = new();

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public List<SkillLevel> Blocked = new();
}
