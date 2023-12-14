using Core.Infrastructure.States;
using VContainer;
using VContainer.Unity;

namespace Core.Infrastructure
{
    public class GameBootstrapper : IStartable
    {
        private GameStateMachine _stateMachine;
        
        [Inject]
        private void Construct(GameStateMachine stateMachine)
        {
            _stateMachine = stateMachine;
        }

        public void Start()
        {
            _stateMachine.Enter<BootstrapState>();
        }
    }
}