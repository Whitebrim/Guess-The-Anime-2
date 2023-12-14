using Core.Services.AssetManagement;
using VContainer;

namespace Core.Infrastructure.States
{
    public class GameLoopState : IState
    {
        private IAssetProvider _assetProvider;
        private GameStateMachine _stateMachine;

        public void Enter()
        {
        }

        public void Exit()
        {
        }

        [Inject]
        private void Construct(GameStateMachine stateMachine, IAssetProvider assetProvider)
        {
            _stateMachine = stateMachine;
            _assetProvider = assetProvider;
        }
    }
}