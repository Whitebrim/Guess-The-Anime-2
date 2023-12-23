using System;
using System.Threading.Tasks;
using Core.Services;
using LunarConsolePlugin;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;

namespace Core.Infrastructure
{
    public class UnityEntryPoint : MonoBehaviour, ICoroutineRunner
    {
        private static bool _isInitialized;

        [SerializeField] private LunarConsole lunarConsole;

        private async void Awake()
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

            await ApplicationInit();

            _isInitialized = true;
        }

        private async Task ApplicationInit()
        {
            Application.targetFrameRate = (int)Math.Max(Screen.currentResolution.refreshRateRatio.value, 60);
            Addressables.InitializeAsync();
#if LUNAR_CONSOLE_ENABLED
            lunarConsole.InitInstance();
#endif
            await UnityServices.InitializeAsync();
        }
    }
}