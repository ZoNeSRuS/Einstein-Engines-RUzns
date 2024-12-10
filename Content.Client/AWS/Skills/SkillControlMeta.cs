using Content.Shared.AWS.Skills;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.AWS.Skills
{
    internal sealed class SkillControlMeta
    {
        public static readonly AttachedProperty<SkillControlMeta> SkillMetaProperty =
        AttachedProperty<SkillControlMeta>.Create("SkillMetaProperty", typeof(Control), defaultValue: new SkillControlMeta());

        public ProtoId<SkillPrototype> SkillId { get; }
        public SkillLevel Level { get; }

        public SkillControlMeta(ProtoId<SkillPrototype> skillId, SkillLevel level)
        {
            SkillId = skillId;
            Level = level;
        }
        private SkillControlMeta()
        {

        }
    }
}
