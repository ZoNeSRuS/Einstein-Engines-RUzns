using Content.Shared.Ame.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.AWS.Skills
{
    [UsedImplicitly]
    public sealed class SkillsBoundUserInterface : BoundUserInterface
    {
        private SkillsWindow? _window;

        public SkillsBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {

        }

        protected override void Open()
        {
            base.Open();

            _window = this.CreateWindow<SkillsWindow>();

        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            var castState = (AmeControllerBoundUserInterfaceState) state;
            _window?.UpdateState(castState); //Update window state
        }

        public void ButtonPressed(UiButton button)
        {
            SendMessage(new UiButtonPressedMessage(button));
        }
    }
}
