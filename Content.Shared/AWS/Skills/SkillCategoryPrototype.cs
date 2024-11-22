using Robust.Shared.Prototypes;

namespace Content.Shared.AWS.Skills;

[Prototype("skillCategory")]
public sealed partial class SkillCategoryPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = string.Empty;
}
