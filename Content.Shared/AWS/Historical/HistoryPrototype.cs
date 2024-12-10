using Robust.Shared.Prototypes;
using Content.Shared.AWS.Skills;

namespace Content.Shared.AWS.Historical;

[Prototype("history")]
public sealed partial class HistoryPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = string.Empty;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public bool IsDefault { get; set; } = false;

    [ViewVariables(VVAccess.ReadWrite), DataField(required: true)]
    public Enum HistoryType { get; set; } = default!;

    [ViewVariables(VVAccess.ReadWrite), DataField(required: true)]
    public string Name = string.Empty;

    [ViewVariables(VVAccess.ReadWrite), DataField(required: true)]
    public string Description = string.Empty;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public ProtoId<SkillCategoryPrototype> BlockedForSpecies = new();

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public List<ProtoId<HistoryPrototype>> BlockingHistories = new();

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public SkillContainer Container = new();
}

public enum HistoryType
{
    Culture,
    Lifestyle,
    Faction,
}
