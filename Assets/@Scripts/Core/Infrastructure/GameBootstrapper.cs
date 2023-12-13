using Core.Infrastructure.States;
using Core.Services;
using Core.Services.Audio;
using LunarConsolePlugin;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using VContainer;

namespace Core.Infrastructure
{
    [RequireComponent(typeof(MainThreadDispatcher), typeof(AudioSystem))]
    public class GameBootstrapper : MonoBehaviour, ICoroutineRunner
    {
        public static bool IsInitialized;

        [SerializeField] private LunarConsole lunarConsole;
        
        private GameStateMachine _stateMachine;

        [Inject]
        private void Construct(GameStateMachine stateMachine)
        {
            _stateMachine = stateMachine;
        }

        private void Awake()
        {
            if (SceneManager.GetActiveScene().buildIndex != 0)
            {
                if (!IsInitialized) SceneManager.LoadScene(0);
                Destroy(gameObject);
                return;
            }
            DontDestroyOnLoad(this);

            ApplicationInit();

            IsInitialized = true;

            _stateMachine.Enter<BootstrapState>();
        }

        private void ApplicationInit()
        {
            UnityEngine.Application.targetFrameRate = Mathf.Max(Screen.currentResolution.refreshRate, 60);
            Addressables.InitializeAsync();
            lunarConsole.InitInstance();
        }
    }
}