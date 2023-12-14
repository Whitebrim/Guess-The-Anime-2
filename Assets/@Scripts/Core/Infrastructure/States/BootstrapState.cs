using VContainer;

namespace Core.Infrastructure.States
{
    public class BootstrapState : IState
    {
        private GameStateMachine _stateMachine;

        public void Enter()
        {
            EnterLoadLevel();
        }

        public void Exit()
        {
        }

        [Inject]
        public void Construct(GameStateMachine stateMachine)
        {
            _stateMachine = stateMachine;
        }

        private void EnterLoadLevel()
        {
            _stateMachine.Enter<LoadLevelState, string>(SceneName.MainMenu);
        }
    }
}