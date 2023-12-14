using Core.Services.SceneLoader;
using VContainer;

namespace Core.Infrastructure.States
{
    public class LoadLevelState : IPayloadedState<string>
    {
        private ISceneLoader _sceneLoader;
        private GameStateMachine _stateMachine;

        public void Enter(string sceneName)
        {
            _sceneLoader.Load(sceneName, OnLoaded);
        }

        public void Exit()
        {
        }

        [Inject]
        private void Construct(GameStateMachine stateMachine, ISceneLoader sceneLoader)
        {
            _stateMachine = stateMachine;
            _sceneLoader = sceneLoader;
        }

        private void OnLoaded()
        {
            //_stateMachine.Enter<GameLoopState>();
        }
    }
}