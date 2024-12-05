using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.AWS.Skills;

public class SkillPointController
{
    public uint MaxPoints { get; set; }
    public uint CurrentPoints { get => SumEstablishedPoints(); }
    public Action<ProtoId<SkillPrototype>, SkillLevel>? OnRecalculateSkill;
    private SkillContainer Container { get; }
    private readonly IPrototypeManager _prototypeManager;

    private void CopyUnblocked(Dictionary<ProtoId<SkillPrototype>, List<Enum>> from, Dictionary<ProtoId<SkillPrototype>, List<Enum>> to)
    {
        foreach (var (key, value) in from)
            to[key] = new List<Enum>(value);
    }

    public SkillLevel GetCurrentSkillLevel(ProtoId<SkillPrototype> protoId)
    {
        if (Container.Skills.TryGetValue(protoId, out var currentLevel))
            return (SkillLevel)currentLevel;

        return SkillLevel.NonSkilled;
    }

    public bool CanHaveSkillLevel(ProtoId<SkillPrototype> protoId, SkillLevel level)
    {
        if (!_prototypeManager.TryIndex(protoId, out var skillProto))
            return false;

        if (skillProto.Cost.TryGetValue(level, out _))
            return true;

        return false;
    }

    public uint GetPointsForSkill(ProtoId<SkillPrototype> protoId, SkillLevel level)
    {
        if (!_prototypeManager.TryIndex(protoId, out var skillProto))
            return 0;

        var currentSkillLevel = GetCurrentSkillLevel(protoId);
        var selectedSkillCost = skillProto!.Cost[level];

        if (currentSkillLevel == level)
            return selectedSkillCost;

        var currentSkillCost = skillProto!.Cost[currentSkillLevel];

        if (currentSkillCost > selectedSkillCost)
            return currentSkillCost - selectedSkillCost;

        return selectedSkillCost - currentSkillCost;
    }

    public void ProcessSkill(ProtoId<SkillPrototype> protoId, SkillLevel level)
    {
        var skillCost = GetPointsForSkill(protoId, level);

        /*if (Container.UnblockedSkillLevels.)*/

        if (CurrentPoints >= skillCost)
        {
            AddLevelToSkill(protoId, level);
            OnRecalculateSkill?.Invoke(protoId, level);
        }
    }

    private void AddLevelToSkill(ProtoId<SkillPrototype> protoId, SkillLevel level)
        => Container.Skills[protoId] = level;

    private uint SumEstablishedPoints()
    {
        uint currentSkillsCost = 0;

        foreach (var (key, value) in Container.Skills)
            currentSkillsCost += GetPointsForSkill(key, (SkillLevel)value);

        return MaxPoints - currentSkillsCost;
    }

    [Obsolete("You should do this logic in your system")]
    public static (bool, SkillContainer?) IsValid(uint maxPoints, Dictionary<ProtoId<SkillPrototype>, List<Enum>> unblockedSkills, SkillContainer clContainer)
    {
        var skillController = new SkillPointController(maxPoints, unblockedSkills, null, clContainer);

        if (skillController.CurrentPoints >= 0)
            return (true, skillController.Container);

        return (false, null);
    }

    public SkillPointController(uint maxPoints, Dictionary<ProtoId<SkillPrototype>, List<Enum>> unblockedSkills, IPrototypeManager? protoManager, SkillContainer? container)
    {
        Container = container ?? new();
        MaxPoints = maxPoints;

        foreach (var (key, value) in unblockedSkills)
            Container.UnblockedSkillLevels[key] = value;

        _prototypeManager = protoManager ?? IoCManager.Resolve<IPrototypeManager>();
    }
}
