using Core.Services;
using LunarConsolePlugin;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core.Infrastructure
{
    public class UnityEntryPoint : MonoBehaviour, ICoroutineRunner
    {
        private static bool _isInitialized;

        [SerializeField] private LunarConsole lunarConsole;
        
        private void Awake()
        {
            if (SceneManager.GetActiveScene().buildIndex != 0)
            {
                if (!_isInitialized)
                {
                    SceneManager.LoadScene(0);
                }

                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(this);

            ApplicationInit();
            
            _isInitialized = true;
        }
        
        private void ApplicationInit()
        {
            Application.targetFrameRate = (int)System.Math.Max(Screen.currentResolution.refreshRateRatio.value, 60);
            UnityEngine.AddressableAssets.Addressables.InitializeAsync();
            lunarConsole.InitInstance();
        }
    }
}
