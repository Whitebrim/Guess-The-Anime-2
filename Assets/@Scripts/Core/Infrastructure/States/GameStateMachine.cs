using VContainer;

namespace Core.Infrastructure.States
{
    public class GameStateMachine
    {
        private IBaseState _currentState;
        private IObjectResolver _resolver;

        [Inject]
        private void Construct(IObjectResolver resolver)
        {
            _resolver = resolver;
        }

        public void Enter<TState>() where TState : class, IState
        {
            IState state = ChangeState<TState>();
            state.Enter();
        }

        public void Enter<TState, TPayload>(TPayload payload) where TState : class, IPayloadedState<TPayload>
        {
            var state = ChangeState<TState>();
            state.Enter(payload);
        }

        private TState GetState<TState>() where TState : class, IBaseState
        {
            return _resolver.Resolve<TState>();
        }

        private TState ChangeState<TState>() where TState : class, IBaseState
        {
            _currentState?.Exit();

            var state = GetState<TState>();
            _currentState = state;

            return state;
        }
    }
}