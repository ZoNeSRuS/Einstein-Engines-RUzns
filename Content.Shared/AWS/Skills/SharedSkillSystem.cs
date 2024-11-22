using Content.Shared.Humanoid;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Shared.AWS.Skills;

public abstract class SharedSkillSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    const bool lowpop = false; // условно пока так будет окда

    public override void Initialize()
    {
        base.Initialize();
    }

    [PublicAPI]
    public bool CanReachSkillLevel(HumanoidAppearanceComponent component, ProtoId<SkillPrototype> skillName, SkillLevel level)
    {
        return false;
    }

    [PublicAPI]
    public SkillLevel GetSkillLevel(EntityUid ent, ProtoId<SkillPrototype> skillName)
    {
        if (lowpop)
            return SkillLevel.Trained;

        if (TryComp<CharacterSkillComponent>(ent, out var comp) && comp.Container is not null)
            if (comp.Container.Skills.TryGetValue(skillName, out var skillLevel))
                return skillLevel;

        return SkillLevel.NonSkilled;
    }

    [PublicAPI]
    public ReadOnlyDictionary<ProtoId<SkillPrototype>, SkillLevel> GetSkills(EntityUid ent)
    {
        if (TryComp<CharacterSkillComponent>(ent, out var comp) && comp.Container is not null)
            return comp.Container.Skills.AsReadOnly();

        return new Dictionary<ProtoId<SkillPrototype>, SkillLevel>().AsReadOnly();
    }

    [PublicAPI]
    public ReadOnlyCollection<SkillCategoryPrototype> GetCategories()
    {
        return _prototypeManager.GetInstances<SkillCategoryPrototype>().Values.AsReadOnly();
    }

    [PublicAPI]
    public ReadOnlyCollection<SkillPrototype> GetSkills()
    {
        return _prototypeManager.GetInstances<SkillPrototype>().Values.AsReadOnly();
    }

    [PublicAPI]
    public bool IsSkillBlocked(EntityUid ent, ProtoId<SkillPrototype> skillName, SkillLevel skillLevel, [NotNullWhen(true)] out string? error)
    {
        error = null;
        return false;
    }

    [PublicAPI]
    public bool TrySetSkillLevel(EntityUid ent, ProtoId<SkillPrototype> skillName, SkillLevel skillLevel, [NotNullWhen(false)] out string? error)
    {
        error = null;

        if (!TryComp<CharacterSkillComponent>(ent, out var comp))
        {
            error = "cannot have skills";
            return false;
        }

        if (IsSkillBlocked(ent, skillName, skillLevel, out error))
            return false;

        SetSkillLevel((ent, comp), skillName, skillLevel);
        return true;
    }

    private void SetSkillLevel(Entity<CharacterSkillComponent> ent, ProtoId<SkillPrototype> skillName, SkillLevel skillLevel)
    {
        if (ent.Comp.Container is not null)
            ent.Comp.Container.Skills[skillName] = skillLevel;
    }
}
