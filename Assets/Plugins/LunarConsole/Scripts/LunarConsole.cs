//
//  LunarConsole.cs
//
//  Lunar Unity Mobile Console
//  https://github.com/SpaceMadness/lunar-unity-console
//
//  Copyright 2015-2021 Alex Lementuev, SpaceMadness.
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
//


#define LUNAR_CONSOLE_ENABLED
#define LUNAR_CONSOLE_FULL

#if UNITY_IOS || UNITY_IPHONE || UNITY_ANDROID || UNITY_EDITOR
#define LUNAR_CONSOLE_PLATFORM_SUPPORTED
#endif

#if LUNAR_CONSOLE_ENABLED && !LUNAR_CONSOLE_ANALYTICS_DISABLED
#define LUNAR_CONSOLE_ANALYTICS_ENABLED
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using LunarConsolePluginInternal;
using UnityEngine;
using Debug = UnityEngine.Debug;
#if UNITY_EDITOR
using System.Runtime.CompilerServices;
#endif

#if UNITY_EDITOR
[assembly: InternalsVisibleTo("Test")]
#endif

namespace LunarConsolePlugin
{
    public enum Gesture
    {
        None,
        SwipeDown
    }

    internal delegate void LunarConsoleNativeMessageCallback(string message);

    internal delegate void LunarConsoleNativeMessageHandler(IDictionary<string, string> data);

    [Serializable]
    public class LogEntryColors
    {
        [SerializeField] public Color32 foreground;

        [SerializeField] public Color32 background;
    }

    [Serializable]
    public class LogOverlayColors
    {
        [SerializeField] public LogEntryColors exception = MakeColors(0xFFEA4646, 0xFF1E1E1E);

        [SerializeField] public LogEntryColors error = MakeColors(0xFFEA4646, 0xFF1E1E1E);

        [SerializeField] public LogEntryColors warning = MakeColors(0xFFCBCB40, 0xFF1E1E1E);

        [SerializeField] public LogEntryColors debug = MakeColors(0xFF9BDDFF, 0xFF1E1E1E);

        private static LogEntryColors MakeColors(uint foreground, uint background)
        {
            var colors = new LogEntryColors();
            colors.foreground = MakeColor(foreground);
            colors.background = MakeColor(background);
            return colors;
        }

        private static Color32 MakeColor(uint argb)
        {
            var a = (byte)((argb >> 24) & 0xff);
            var r = (byte)((argb >> 16) & 0xff);
            var g = (byte)((argb >> 8) & 0xff);
            var b = (byte)(argb & 0xff);
            return new Color32(r, g, b, a);
        }
    }

    public enum ExceptionWarningDisplayMode
    {
        None,
        Errors,
        Exceptions,
        All
    }

    [Serializable]
    public class ExceptionWarningSettings
    {
        [SerializeField] public ExceptionWarningDisplayMode displayMode = ExceptionWarningDisplayMode.All;
    }

    [Serializable]
    public class LogOverlaySettings
    {
        [SerializeField] public bool enabled;

        [SerializeField] [Tooltip("Maximum visible lines count")]
        public int maxVisibleLines = 3;

        [SerializeField] [Tooltip("The amount of time each line would be displayed")]
        public float timeout = 1.0f;

        [SerializeField] public LogOverlayColors colors = new();
    }

    [Serializable]
    public class LunarConsoleSettings
    {
        [SerializeField] public ExceptionWarningSettings exceptionWarning = new();

#if LUNAR_CONSOLE_FREE
        [HideInInspector]
#endif
        [SerializeField] public LogOverlaySettings logOverlay = new();

        [Range(128, 65536)] [Tooltip("Log output will never become bigger than this capacity")] [SerializeField]
        public int capacity = 4096;

        [Range(128, 65536)] [Tooltip("Log output will be trimmed this many lines when overflown")] [SerializeField]
        public int trim = 512;

        [Tooltip("Gesture type to open the console")] [SerializeField]
        public Gesture gesture = Gesture.SwipeDown;

        [Tooltip("If checked - enables Unity Rich Text in log output")] [SerializeField]
        public bool richTextTags;

#if LUNAR_CONSOLE_FREE
        [HideInInspector]
#endif
        [SerializeField] public bool sortActions = true;

#if LUNAR_CONSOLE_FREE
        [HideInInspector]
#endif
        [SerializeField] public bool sortVariables = true;

        [SerializeField] public string[] emails;
    }

    public sealed class LunarConsole : MonoBehaviour
    {
#pragma warning disable 0649
#pragma warning disable 0414

        [SerializeField] private LunarConsoleSettings m_settings = new();

        private bool m_variablesDirty;

#pragma warning restore 0649
#pragma warning restore 0414

#if LUNAR_CONSOLE_ENABLED

        private IPlatform m_platform;

        private IDictionary<string, LunarConsoleNativeMessageHandler> m_nativeHandlerLookup;

        #region Life cycle

        private void OnEnable()
        {
            EnablePlatform();
        }

        private void OnDisable()
        {
            DisablePlatform();
        }

        private void Update()
        {
            if (m_platform != null)
            {
                m_platform.Update();
            }

            if (m_variablesDirty)
            {
                m_variablesDirty = false;
                SaveVariables();
            }
        }

        private void OnDestroy()
        {
            DestroyInstance();
        }

        #endregion

        #region Plugin Lifecycle

        public void InitInstance()
        {
            if (instance == null)
            {
                if (IsPlatformSupported())
                {
                    instance = this;
                    Log.dev("Instance created...");
                }
                else
                {
                    //Destroy(gameObject);
                    Log.dev("Platform not supported. Destroying object...");
                }
            }
            else if (instance != this)
            {
                //Destroy(gameObject);
                Log.dev("Another instance exists. Destroying object...");
            }
        }

        private void EnablePlatform()
        {
            if (instance != null)
            {
                bool succeed = InitPlatform(m_settings);
                Log.dev("Platform initialized successfully: {0}", succeed.ToString());
            }
        }

        private void DisablePlatform()
        {
            if (instance != null)
            {
                bool succeed = DestroyPlatform();
                Log.dev("Platform destroyed successfully: {0}", succeed.ToString());
            }
        }

        private static bool IsPlatformSupported()
        {
#if UNITY_EDITOR
            return true;
#elif UNITY_IOS || UNITY_IPHONE
            return Application.platform == RuntimePlatform.IPhonePlayer;
#elif UNITY_ANDROID
            return Application.platform == RuntimePlatform.Android;
#else
            return false;
#endif
        }

        #endregion

        #region Platforms

        private bool InitPlatform(LunarConsoleSettings settings)
        {
            try
            {
                if (m_platform == null)
                {
                    m_platform = CreatePlatform(settings);
                    if (m_platform != null)
                    {
                        registry = new CRegistry();
                        registry.registryDelegate = m_platform;

                        Application.logMessageReceivedThreaded += OnLogMessageReceived;

#if LUNAR_CONSOLE_FULL
                        ResolveVariables();
                        LoadVariables();
#endif // LUNAR_CONSOLE_FULL

                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                Log.e(e, "Can't init platform");
            }

            return false;
        }

        private bool DestroyPlatform()
        {
            if (m_platform != null)
            {
                Application.logMessageReceivedThreaded -= OnLogMessageReceived;

                if (registry != null)
                {
                    registry.Destroy();
                    registry = null;
                }

                m_platform.Destroy();
                m_platform = null;

                return true;
            }

            return false;
        }

        private IPlatform CreatePlatform(LunarConsoleSettings settings)
        {
#if UNITY_IOS || UNITY_IPHONE
            if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                LunarConsoleNativeMessageCallback callback = NativeMessageCallback;
                return new PlatformIOS(gameObject.name, callback.Method.Name, Constants.Version, settings);
            }
#elif UNITY_ANDROID
            if (Application.platform == RuntimePlatform.Android)
            {
                LunarConsoleNativeMessageCallback callback = NativeMessageCallback;
                return new PlatformAndroid(gameObject.name, callback.Method.Name, Constants.Version, settings);
            }
#endif

#if UNITY_EDITOR
            return new PlatformEditor();
#else
            return null;
#endif
        }

        private void DestroyInstance()
        {
            if (instance == this)
            {
                DestroyPlatform();
                instance = null;
            }
        }

        private static string GetGestureName(Gesture gesture)
        {
            return gesture.ToString();
        }

        private interface IPlatform : ICRegistryDelegate
        {
            void Update();
            void OnLogMessageReceived(string message, string stackTrace, LogType type);
            bool ShowConsole();
            bool HideConsole();
            void ClearConsole();
            void Destroy();
        }

        #region CVar resolver

        private void ResolveVariables()
        {
            try
            {
                foreach (Assembly assembly in ListAssemblies())
                {
                    Log.dev("Checking '{0}'...", assembly);

                    try
                    {
                        List<Type> containerTypes = ReflectionUtils.FindAttributeTypes<CVarContainerAttribute>(assembly);
                        foreach (Type type in containerTypes)
                        {
                            RegisterVariables(type);
                        }
                    }
                    catch (Exception e)
                    {
                        Log.e(e, "Unable to register variables from assembly: {0}", assembly);
                    }
                }
            }
            catch (Exception e)
            {
                Log.e(e, "Unable to register variables");
            }
        }

        private static IList<Assembly> ListAssemblies()
        {
            return ReflectionUtils.ListAssemblies(assembly =>
            {
                string assemblyName = assembly.FullName;
                return !assemblyName.StartsWith("Unity") &&
                       !assemblyName.StartsWith("System") &&
                       !assemblyName.StartsWith("Microsoft") &&
                       !assemblyName.StartsWith("SyntaxTree") &&
                       !assemblyName.StartsWith("Mono") &&
                       !assemblyName.StartsWith("ExCSS") &&
                       !assemblyName.StartsWith("nunit") &&
                       !assemblyName.StartsWith("netstandard") &&
                       !assemblyName.StartsWith("mscorlib") &&
                       assemblyName != "Accessibility";
            });
        }

        private void RegisterVariables(Type type)
        {
            try
            {
                FieldInfo[] fields = type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (fields.Length > 0)
                {
                    foreach (FieldInfo field in fields)
                    {
                        if (!field.FieldType.IsAssignableFrom(typeof(CVar)) && !field.FieldType.IsSubclassOf(typeof(CVar)))
                        {
                            continue;
                        }

                        var cvar = field.GetValue(null) as CVar;
                        if (cvar == null)
                        {
                            Log.w("Unable to register variable {0}.{0}", type.Name, field.Name);
                            continue;
                        }

                        CVarValueRange variableRange = ResolveVariableRange(field);
                        if (variableRange.IsValid)
                        {
                            if (cvar.Type == CVarType.Float)
                            {
                                cvar.Range = variableRange;
                            }
                            else
                            {
                                Log.w("'{0}' attribute is only available with 'float' variables", typeof(CVarRangeAttribute).Name);
                            }
                        }

                        registry.Register(cvar);
                    }
                }
            }
            catch (Exception e)
            {
                Log.e(e, "Unable to initialize cvar container: {0}", type);
            }
        }

        private static CVarValueRange ResolveVariableRange(FieldInfo field)
        {
            try
            {
                object[] attributes = field.GetCustomAttributes(typeof(CVarRangeAttribute), true);
                if (attributes.Length > 0)
                {
                    var rangeAttribute = attributes[0] as CVarRangeAttribute;
                    if (rangeAttribute != null)
                    {
                        float min = rangeAttribute.min;
                        float max = rangeAttribute.max;
                        if (max - min < 0.00001f)
                        {
                            Log.w("Invalid range [{0}, {1}] for variable '{2}'", min.ToString(), max.ToString(), field.Name);
                            return CVarValueRange.Undefined;
                        }

                        return new CVarValueRange(min, max);
                    }
                }
            }
            catch (Exception e)
            {
                Log.e(e, "Exception while resolving variable's range: {0}", field.Name);
            }

            return CVarValueRange.Undefined;
        }

        private void LoadVariables()
        {
            try
            {
                string configPath = Path.Combine(Application.persistentDataPath, "lunar-mobile-console-variables.bin");
                if (File.Exists(configPath))
                {
                    Log.dev("Loading variables from file {0}", configPath);
                    using (FileStream stream = File.OpenRead(configPath))
                    {
                        using (var reader = new BinaryReader(stream))
                        {
                            int count = reader.ReadInt32();
                            for (var i = 0; i < count; ++i)
                            {
                                string name = reader.ReadString();
                                string value = reader.ReadString();
                                CVar cvar = registry.FindVariable(name);
                                if (cvar == null)
                                {
                                    Log.w("Variable '{0}' not registered. Ignoring...", name);
                                    continue;
                                }

                                cvar.Value = value;
                                m_platform.OnVariableUpdated(registry, cvar);
                            }
                        }
                    }
                }
                else
                {
                    Log.dev("Missing variables file {0}", configPath);
                }
            }
            catch (Exception e)
            {
                Log.e(e, "Error while loading variables");
            }
        }

        private void SaveVariables()
        {
            try
            {
                string configPath = Path.Combine(Application.persistentDataPath, "lunar-mobile-console-variables.bin");
                Log.dev("Saving variables to file {0}", configPath);
                using (FileStream stream = File.OpenWrite(configPath))
                {
                    using (var writer = new BinaryWriter(stream))
                    {
                        CVarList cvars = registry.cvars;
                        var count = 0;
                        foreach (CVar cvar in cvars)
                        {
                            if (ShouldSaveVar(cvar))
                            {
                                ++count;
                            }
                        }

                        writer.Write(count);
                        foreach (CVar cvar in cvars)
                        {
                            if (ShouldSaveVar(cvar))
                            {
                                writer.Write(cvar.Name);
                                writer.Write(cvar.Value);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.e(e, "Error while saving variables");
            }
        }

        private bool ShouldSaveVar(CVar cvar)
        {
            return !cvar.IsDefault && !cvar.HasFlag(CFlags.NoArchive);
        }

        #endregion

        #region Messages

        private void OnLogMessageReceived(string message, string stackTrace, LogType type)
        {
            m_platform.OnLogMessageReceived(message, stackTrace, type);
        }

        #endregion

#if UNITY_IOS || UNITY_IPHONE
        class PlatformIOS : IPlatform
        {
            [DllImport("__Internal")]
            private static extern void __lunar_console_initialize(string targetName, string methodName, string version, string settingsJson);

            [DllImport("__Internal")]
            private static extern void __lunar_console_log_message(string message, string stackTrace, int type);

            [DllImport("__Internal")]
            private static extern void __lunar_console_show();

            [DllImport("__Internal")]
            private static extern void __lunar_console_hide();

            [DllImport("__Internal")]
            private static extern void __lunar_console_clear();

            [DllImport("__Internal")]
            private static extern void __lunar_console_action_register(int actionId, string name);

            [DllImport("__Internal")]
            private static extern void __lunar_console_action_unregister(int actionId);

            [DllImport("__Internal")]
            private static extern void __lunar_console_cvar_register(int variableId, string name, string type, string value, string defaultValue, int flags, bool hasRange, float min, float max, string values);

            [DllImport("__Internal")]
            private static extern void __lunar_console_cvar_update(int variableId, string value);

            [DllImport("__Internal")]
            private static extern void __lunar_console_destroy();

            /// <summary>
            /// Initializes a new instance of the iOS platform class.
            /// </summary>
            /// <param name="targetName">The name of the game object which will receive native callbacks</param>
            /// <param name="methodName">The method of the game object which will be called from the native code</param>
            /// <param name="version">Plugin version</param>
            /// <param name="settings">Plugin settings</param>
            public PlatformIOS(string targetName, string methodName, string version, LunarConsoleSettings settings)
            {
                var settingsJson = JsonUtility.ToJson(settings);
                __lunar_console_initialize(targetName, methodName, version, settingsJson);
            }

            public void Update()
            {
            }

            public void OnLogMessageReceived(string message, string stackTrace, LogType type)
            {
                // Suppress "stale touch" warning.
                // See: https://github.com/SpaceMadness/lunar-unity-console/issues/70 
                if (type == LogType.Error && message == "Stale touch detected!")
                {
                    return;
                }
                __lunar_console_log_message(message, stackTrace, (int)type);
            }

            public bool ShowConsole()
            {
                __lunar_console_show();
                return true;
            }

            public bool HideConsole()
            {
                __lunar_console_hide();
                return true;
            }

            public void ClearConsole()
            {
                __lunar_console_clear();
            }

            public void OnActionRegistered(CRegistry registry, CAction action)
            {
                __lunar_console_action_register(action.Id, action.Name);
            }

            public void OnActionUnregistered(CRegistry registry, CAction action)
            {
                __lunar_console_action_unregister(action.Id);
            }

            public void OnVariableRegistered(CRegistry registry, CVar cvar)
            {
                string values = cvar.Type == CVarType.Enum ? cvar.AvailableValues.Join(",") : null;
                __lunar_console_cvar_register(cvar.Id, cvar.Name, cvar.Type.ToString(), cvar.Value, cvar.DefaultValue, (int)cvar.Flags, cvar.HasRange, cvar.Range.min, cvar.Range.max, values);
            }

            public void OnVariableUpdated(CRegistry registry, CVar cvar)
            {
                __lunar_console_cvar_update(cvar.Id, cvar.Value);
            }

            public void Destroy()
            {
                __lunar_console_destroy();
            }
        }

#elif UNITY_ANDROID

        private class PlatformAndroid : IPlatform
        {
            private static readonly string kPluginClassName = "spacemadness.com.lunarconsole.console.NativeBridge";

            private readonly jvalue[] m_args0 = new jvalue[0];
            private readonly jvalue[] m_args1 = new jvalue[1];
            private readonly jvalue[] m_args10 = new jvalue[10];
            private readonly jvalue[] m_args2 = new jvalue[2];
            private readonly jvalue[] m_args3 = new jvalue[3];
            private readonly int m_mainThreadId;

            private readonly Queue<LogMessageEntry> m_messageQueue;
            private readonly IntPtr m_methodClearConsole;
            private readonly IntPtr m_methodDestroy;
            private readonly IntPtr m_methodHideConsole;
            private readonly IntPtr m_methodLogMessage;
            private readonly IntPtr m_methodRegisterAction;
            private readonly IntPtr m_methodRegisterVariable;
            private readonly IntPtr m_methodShowConsole;
            private readonly IntPtr m_methodUnregisterAction;
            private readonly IntPtr m_methodUpdateVariable;

            private readonly AndroidJavaClass m_pluginClass;

            private readonly IntPtr m_pluginClassRaw;

            /// <summary>
            ///     Initializes a new instance of the Android platform class.
            /// </summary>
            /// <param name="targetName">The name of the game object which will receive native callbacks</param>
            /// <param name="methodName">The method of the game object which will be called from the native code</param>
            /// <param name="version">Plugin version</param>
            /// <param name="settings">Plugin settings</param>
            public PlatformAndroid(string targetName, string methodName, string version, LunarConsoleSettings settings)
            {
                string settingsJson = JsonUtility.ToJson(settings);

                m_mainThreadId = Thread.CurrentThread.ManagedThreadId;
                m_pluginClass = new AndroidJavaClass(kPluginClassName);
                m_pluginClassRaw = m_pluginClass.GetRawClass();

                IntPtr methodInit = GetStaticMethod(m_pluginClassRaw, "init",
                    "(Ljava.lang.String;Ljava.lang.String;Ljava.lang.String;Ljava.lang.String;)V");
                jvalue[] methodInitParams =
                {
                    jval(targetName),
                    jval(methodName),
                    jval(version),
                    jval(settingsJson)
                };
                CallStaticVoidMethod(methodInit, methodInitParams);

                AndroidJNI.DeleteLocalRef(methodInitParams[0].l);
                AndroidJNI.DeleteLocalRef(methodInitParams[1].l);
                AndroidJNI.DeleteLocalRef(methodInitParams[2].l);
                AndroidJNI.DeleteLocalRef(methodInitParams[3].l);

                m_methodLogMessage = GetStaticMethod(m_pluginClassRaw, "logMessage", "(Ljava.lang.String;Ljava.lang.String;I)V");
                m_methodShowConsole = GetStaticMethod(m_pluginClassRaw, "showConsole", "()V");
                m_methodHideConsole = GetStaticMethod(m_pluginClassRaw, "hideConsole", "()V");
                m_methodClearConsole = GetStaticMethod(m_pluginClassRaw, "clearConsole", "()V");
                m_methodRegisterAction = GetStaticMethod(m_pluginClassRaw, "registerAction", "(ILjava.lang.String;)V");
                m_methodUnregisterAction = GetStaticMethod(m_pluginClassRaw, "unregisterAction", "(I)V");
                m_methodRegisterVariable = GetStaticMethod(m_pluginClassRaw, "registerVariable",
                    "(ILjava/lang/String;Ljava/lang/String;Ljava/lang/String;Ljava/lang/String;IZFFLjava/lang/String;)V");
                m_methodUpdateVariable = GetStaticMethod(m_pluginClassRaw, "updateVariable", "(ILjava/lang/String;)V");
                m_methodDestroy = GetStaticMethod(m_pluginClassRaw, "destroy", "()V");

                m_messageQueue = new Queue<LogMessageEntry>();
            }

            ~PlatformAndroid()
            {
                m_pluginClass.Dispose();
            }

            #region IPlatform implementation

            public void Update()
            {
                lock (m_messageQueue)
                {
                    while (m_messageQueue.Count > 0)
                    {
                        LogMessageEntry entry = m_messageQueue.Dequeue();
                        OnLogMessageReceived(entry.message, entry.stackTrace, entry.type);
                    }
                }
            }

            public void OnLogMessageReceived(string message, string stackTrace, LogType type)
            {
                if (Thread.CurrentThread.ManagedThreadId == m_mainThreadId)
                {
                    m_args3[0] = jval(message);
                    m_args3[1] = jval(stackTrace);
                    m_args3[2] = jval((int)type);

                    CallStaticVoidMethod(m_methodLogMessage, m_args3);

                    AndroidJNI.DeleteLocalRef(m_args3[0].l);
                    AndroidJNI.DeleteLocalRef(m_args3[1].l);
                }
                else
                {
                    lock (m_messageQueue)
                    {
                        m_messageQueue.Enqueue(new LogMessageEntry(message, stackTrace, type));
                    }
                }
            }

            public bool ShowConsole()
            {
                try
                {
                    CallStaticVoidMethod(m_methodShowConsole, m_args0);
                    return true;
                }
                catch (Exception e)
                {
                    Debug.LogError("Exception while calling 'LunarConsole.ShowConsole': " + e.Message);
                    return false;
                }
            }

            public bool HideConsole()
            {
                try
                {
                    CallStaticVoidMethod(m_methodHideConsole, m_args0);
                    return true;
                }
                catch (Exception e)
                {
                    Debug.LogError("Exception while calling 'LunarConsole.HideConsole': " + e.Message);
                    return false;
                }
            }

            public void ClearConsole()
            {
                try
                {
                    CallStaticVoidMethod(m_methodClearConsole, m_args0);
                }
                catch (Exception e)
                {
                    Debug.LogError("Exception while calling 'LunarConsole.ClearConsole': " + e.Message);
                }
            }

            public void Destroy()
            {
                try
                {
                    CallStaticVoidMethod(m_methodDestroy, m_args0);
                }
                catch (Exception e)
                {
                    Debug.LogError("Exception while destroying platform: " + e.Message);
                }
            }

            public void OnActionRegistered(CRegistry registry, CAction action)
            {
                try
                {
                    m_args2[0] = jval(action.Id);
                    m_args2[1] = jval(action.Name);
                    CallStaticVoidMethod(m_methodRegisterAction, m_args2);
                    AndroidJNI.DeleteLocalRef(m_args2[1].l);
                }
                catch (Exception e)
                {
                    Debug.LogError("Exception while calling 'LunarConsole.OnActionRegistered': " + e.Message);
                }
            }

            public void OnActionUnregistered(CRegistry registry, CAction action)
            {
                try
                {
                    m_args1[0] = jval(action.Id);
                    CallStaticVoidMethod(m_methodUnregisterAction, m_args1);
                }
                catch (Exception e)
                {
                    Debug.LogError("Exception while calling 'LunarConsole.OnActionUnregistered': " + e.Message);
                }
            }

            public void OnVariableRegistered(CRegistry registry, CVar cvar)
            {
                try
                {
                    m_args10[0] = jval(cvar.Id);
                    m_args10[1] = jval(cvar.Name);
                    m_args10[2] = jval(cvar.Type.ToString());
                    m_args10[3] = jval(cvar.Value);
                    m_args10[4] = jval(cvar.DefaultValue);
                    m_args10[5] = jval((int)cvar.Flags);
                    m_args10[6] = jval(cvar.HasRange);
                    m_args10[7] = jval(cvar.Range.min);
                    m_args10[8] = jval(cvar.Range.max);
                    m_args10[9] = jval(cvar.AvailableValues != null ? cvar.AvailableValues.Join() : null);
                    CallStaticVoidMethod(m_methodRegisterVariable, m_args10);
                    AndroidJNI.DeleteLocalRef(m_args10[1].l);
                    AndroidJNI.DeleteLocalRef(m_args10[2].l);
                    AndroidJNI.DeleteLocalRef(m_args10[3].l);
                    AndroidJNI.DeleteLocalRef(m_args10[4].l);
                    AndroidJNI.DeleteLocalRef(m_args10[9].l);
                }
                catch (Exception e)
                {
                    Debug.LogError("Exception while calling 'LunarConsole.OnVariableRegistered': " + e.Message);
                }
            }

            public void OnVariableUpdated(CRegistry registry, CVar cvar)
            {
                try
                {
                    m_args2[0] = jval(cvar.Id);
                    m_args2[1] = jval(cvar.Value);
                    CallStaticVoidMethod(m_methodUpdateVariable, m_args2);
                    AndroidJNI.DeleteLocalRef(m_args2[1].l);
                }
                catch (Exception e)
                {
                    Debug.LogError("Exception while calling 'LunarConsole.OnVariableUpdated': " + e.Message);
                }
            }

            #endregion

            #region Helpers

            private static IntPtr GetStaticMethod(IntPtr classRaw, string name, string signature)
            {
                return AndroidJNIHelper.GetMethodID(classRaw, name, signature, true);
            }

            private void CallStaticVoidMethod(IntPtr method, jvalue[] args)
            {
                AndroidJNI.CallStaticVoidMethod(m_pluginClassRaw, method, args);
            }

            private bool CallStaticBoolMethod(IntPtr method, jvalue[] args)
            {
                return AndroidJNI.CallStaticBooleanMethod(m_pluginClassRaw, method, args);
            }

            private jvalue jval(string value)
            {
                var val = new jvalue();
                val.l = AndroidJNI.NewStringUTF(value);
                return val;
            }

            private jvalue jval(bool value)
            {
                var val = new jvalue();
                val.z = value;
                return val;
            }

            private jvalue jval(int value)
            {
                var val = new jvalue();
                val.i = value;
                return val;
            }

            private jvalue jval(float value)
            {
                var val = new jvalue();
                val.f = value;
                return val;
            }

            #endregion
        }

        private struct LogMessageEntry
        {
            public readonly string message;
            public readonly string stackTrace;
            public readonly LogType type;

            public LogMessageEntry(string message, string stackTrace, LogType type)
            {
                this.message = message;
                this.stackTrace = stackTrace;
                this.type = type;
            }
        }

#endif // UNITY_ANDROID

#if UNITY_EDITOR

        private class PlatformEditor : IPlatform
        {
            public void Update()
            {
            }

            public void OnLogMessageReceived(string message, string stackTrace, LogType type)
            {
            }

            public bool ShowConsole()
            {
                return false;
            }

            public bool HideConsole()
            {
                return false;
            }

            public void ClearConsole()
            {
            }

            public void Destroy()
            {
            }

            public void OnActionRegistered(CRegistry registry, CAction action)
            {
            }

            public void OnActionUnregistered(CRegistry registry, CAction action)
            {
            }

            public void OnVariableRegistered(CRegistry registry, CVar cvar)
            {
            }

            public void OnVariableUpdated(CRegistry registry, CVar cvar)
            {
            }
        }

#endif // UNITY_ANDROID

        #endregion

        #region Native callback

        private void NativeMessageCallback(string param)
        {
            IDictionary<string, string> data = StringUtils.DeserializeString(param);
            string name = data["name"];
            if (string.IsNullOrEmpty(name))
            {
                Log.w("Can't handle native callback: 'name' is undefined");
                return;
            }

            LunarConsoleNativeMessageHandler handler;
            if (!nativeHandlerLookup.TryGetValue(name, out handler))
            {
                Log.w("Can't handle native callback: handler not found '" + name + "'");
                return;
            }

            try
            {
                handler(data);
            }
            catch (Exception e)
            {
                Log.e(e, "Exception while handling native callback '{0}'", name);
            }
        }

        private IDictionary<string, LunarConsoleNativeMessageHandler> nativeHandlerLookup
        {
            get
            {
                if (m_nativeHandlerLookup == null)
                {
                    m_nativeHandlerLookup = new Dictionary<string, LunarConsoleNativeMessageHandler>();
                    m_nativeHandlerLookup["console_open"] = ConsoleOpenHandler;
                    m_nativeHandlerLookup["console_close"] = ConsoleCloseHandler;
                    m_nativeHandlerLookup["console_action"] = ConsoleActionHandler;
                    m_nativeHandlerLookup["console_variable_set"] = ConsoleVariableSetHandler;
                    m_nativeHandlerLookup["track_event"] = TrackEventHandler;
                }

                return m_nativeHandlerLookup;
            }
        }

        private void ConsoleOpenHandler(IDictionary<string, string> data)
        {
            if (onConsoleOpened != null)
            {
                onConsoleOpened();
            }

            TrackEvent("Console", "console_open");
        }

        private void ConsoleCloseHandler(IDictionary<string, string> data)
        {
            if (onConsoleClosed != null)
            {
                onConsoleClosed();
            }

            TrackEvent("Console", "console_close");
        }

        private void ConsoleActionHandler(IDictionary<string, string> data)
        {
            string actionIdStr;
            if (!data.TryGetValue("id", out actionIdStr))
            {
                Log.w("Can't run action: data is not properly formatted");
                return;
            }

            int actionId;
            if (!int.TryParse(actionIdStr, out actionId))
            {
                Log.w("Can't run action: invalid ID " + actionIdStr);
                return;
            }

            if (registry == null)
            {
                Log.w("Can't run action: registry is not property initialized");
                return;
            }

            CAction action = registry.FindAction(actionId);
            if (action == null)
            {
                Log.w("Can't run action: ID not found " + actionIdStr);
                return;
            }

            try
            {
                action.Execute();
            }
            catch (Exception e)
            {
                Log.e(e, "Can't run action {0}", action.Name);
            }
        }

        private void ConsoleVariableSetHandler(IDictionary<string, string> data)
        {
            string variableIdStr;
            if (!data.TryGetValue("id", out variableIdStr))
            {
                Log.w("Can't set variable: missing 'id' property");
                return;
            }

            string value;
            if (!data.TryGetValue("value", out value))
            {
                Log.w("Can't set variable: missing 'value' property");
                return;
            }

            int variableId;
            if (!int.TryParse(variableIdStr, out variableId))
            {
                Log.w("Can't set variable: invalid ID " + variableIdStr);
                return;
            }

            if (registry == null)
            {
                Log.w("Can't set variable: registry is not property initialized");
                return;
            }

            CVar variable = registry.FindVariable(variableId);
            if (variable == null)
            {
                Log.w("Can't set variable: ID not found " + variableIdStr);
                return;
            }

            try
            {
                switch (variable.Type)
                {
                    case CVarType.Boolean:
                    {
                        int intValue;
                        if (int.TryParse(value, out intValue) && (intValue == 0 || intValue == 1))
                        {
                            variable.BoolValue = intValue == 1;
                            m_variablesDirty = true;
                        }
                        else
                        {
                            Log.e("Invalid boolean value: '{0}'", value);
                        }

                        break;
                    }
                    case CVarType.Integer:
                    {
                        int intValue;
                        if (int.TryParse(value, out intValue))
                        {
                            variable.IntValue = intValue;
                            m_variablesDirty = true;
                        }
                        else
                        {
                            Log.e("Invalid integer value: '{0}'", value);
                        }

                        break;
                    }
                    case CVarType.Float:
                    {
                        float floatValue;
                        if (float.TryParse(value, out floatValue))
                        {
                            variable.FloatValue = floatValue;
                            m_variablesDirty = true;
                        }
                        else
                        {
                            Log.e("Invalid float value: '{0}'", value);
                        }

                        break;
                    }
                    case CVarType.String:
                    {
                        variable.Value = value;
                        m_variablesDirty = true;
                        break;
                    }
                    case CVarType.Enum:
                    {
                        int index = Array.IndexOf(variable.AvailableValues, variable.Value);
                        if (index != -1)
                        {
                            variable.Value = value;
                            m_variablesDirty = true;
                        }
                        else
                        {
                            Log.e("Unexpected variable '{0}' value: {1}", variable.Name, variable.Value);
                        }

                        break;
                    }
                    default:
                    {
                        Log.e("Unexpected variable type: {0}", variable.Type);
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Log.e(e, "Exception while trying to set variable '{0}'", variable.Name);
            }
        }

        private void TrackEventHandler(IDictionary<string, string> data)
        {
#if LUNAR_CONSOLE_ANALYTICS_ENABLED
            string category;
            if (!data.TryGetValue("category", out category) || category.Length == 0)
            {
                Log.w("Can't track event: missing 'category' parameter");
                return;
            }

            string action;
            if (!data.TryGetValue("action", out action) || action.Length == 0)
            {
                Log.w("Can't track event: missing 'action' parameter");
                return;
            }

            int value = LunarConsoleAnalytics.kUndefinedValue;
            ;
            string valueStr;
            if (data.TryGetValue("value", out valueStr))
            {
                if (!int.TryParse(valueStr, out value))
                {
                    Log.w("Can't track event: invalid 'value' parameter: {0}", valueStr);
                    return;
                }
            }

            LunarConsoleAnalytics.TrackEvent(category, action, value);
#endif // LUNAR_CONSOLE_ANALYTICS_ENABLED
        }

        #region Analytics

        private void TrackEvent(string category, string action, int value = LunarConsoleAnalytics.kUndefinedValue)
        {
#if LUNAR_CONSOLE_ANALYTICS_ENABLED
            StartCoroutine(LunarConsoleAnalytics.TrackEvent(category, action, value));
#endif // LUNAR_CONSOLE_ANALYTICS_ENABLED
        }

        #endregion

        #endregion

#endif // LUNAR_CONSOLE_ENABLED

        #region Public API

        /// <summary>
        ///     Shows the console on top of everything. Does nothing if current platform is not supported or if the plugin is not initialized.
        /// </summary>
        public static void Show()
        {
#if LUNAR_CONSOLE_PLATFORM_SUPPORTED
#if LUNAR_CONSOLE_ENABLED
            if (instance != null)
            {
                instance.ShowConsole();
            }
            else
            {
                Log.w("Can't show console: instance is not initialized. Make sure you've installed it correctly");
            }
#else
            Log.w("Can't show console: plugin is disabled");
#endif
#else
            Log.w("Can't show console: current platform is not supported");
#endif
        }

        /// <summary>
        ///     Hides the console. Does nothing if platform is not supported or if plugin is not initialized.
        /// </summary>
        public static void Hide()
        {
#if LUNAR_CONSOLE_PLATFORM_SUPPORTED
#if LUNAR_CONSOLE_ENABLED
            if (instance != null)
            {
                instance.HideConsole();
            }
            else
            {
                Log.w("Can't hide console: instance is not initialized. Make sure you've installed it correctly");
            }
#else
            Log.w("Can't hide console: plugin is disabled");
#endif
#else
            Log.w("Can't hide console: current platform is not supported");
#endif
        }

        /// <summary>
        ///     Clears log messages. Does nothing if platform is not supported or if plugin is not initialized.
        /// </summary>
        public static void Clear()
        {
#if LUNAR_CONSOLE_PLATFORM_SUPPORTED
#if LUNAR_CONSOLE_ENABLED
            if (instance != null)
            {
                instance.ClearConsole();
            }
            else
            {
                Log.w("Can't clear console: instance is not initialized. Make sure you've installed it correctly");
            }
#else
            Log.w("Can't clear console: plugin is disabled");
#endif
#else
            Log.w("Can't clear console: current platform is not supported");
#endif
        }

        /// <summary>
        ///     Registers a user-defined action with a specific name and callback.
        ///     Does nothing if platform is not supported or if plugin is not initialized.
        /// </summary>
        /// <param name="name">Display name</param>
        /// <param name="action">Callback delegate</param>
        public static void RegisterAction(string name, Action action)
        {
#if LUNAR_CONSOLE_PLATFORM_SUPPORTED
#if LUNAR_CONSOLE_FULL
#if LUNAR_CONSOLE_ENABLED
            if (instance != null)
            {
                instance.RegisterConsoleAction(name, action);
            }
            else
            {
                Log.w("Can't register action: instance is not initialized. Make sure you've installed it correctly");
            }
#else // LUNAR_CONSOLE_ENABLED
            Log.w("Can't register action: plugin is disabled");
#endif // LUNAR_CONSOLE_ENABLED
#else // LUNAR_CONSOLE_FULL
            Log.w("Can't register action: feature is not available in FREE version. Learn more about PRO version: https://goo.gl/TLInmD");
#endif // LUNAR_CONSOLE_FULL
#endif // LUNAR_CONSOLE_PLATFORM_SUPPORTED
        }

        /// <summary>
        ///     Un-registers a user-defined action with a specific callback.
        ///     Does nothing if platform is not supported or if plugin is not initialized.
        /// </summary>
        public static void UnregisterAction(Action action)
        {
#if LUNAR_CONSOLE_PLATFORM_SUPPORTED
#if LUNAR_CONSOLE_FULL
#if LUNAR_CONSOLE_ENABLED
            if (instance != null)
            {
                instance.UnregisterConsoleAction(action);
            }
#endif // LUNAR_CONSOLE_ENABLED
#endif // LUNAR_CONSOLE_FULL
#endif // LUNAR_CONSOLE_PLATFORM_SUPPORTED
        }

        /// <summary>
        ///     Un-registers a user-defined action with a specific name.
        ///     Does nothing if platform is not supported or if plugin is not initialized.
        /// </summary>
        public static void UnregisterAction(string name)
        {
#if LUNAR_CONSOLE_PLATFORM_SUPPORTED
#if LUNAR_CONSOLE_FULL
#if LUNAR_CONSOLE_ENABLED
            if (instance != null)
            {
                instance.UnregisterConsoleAction(name);
            }
#endif // LUNAR_CONSOLE_ENABLED
#endif // LUNAR_CONSOLE_FULL
#endif // LUNAR_CONSOLE_PLATFORM_SUPPORTED
        }

        /// <summary>
        ///     Un-registers all user-defined actions with a specific target
        ///     (the object of the class which contains callback methods).
        ///     Does nothing if platform is not supported or if plugin is not initialized.
        /// </summary>
        public static void UnregisterAllActions(object target)
        {
#if LUNAR_CONSOLE_PLATFORM_SUPPORTED
#if LUNAR_CONSOLE_FULL
#if LUNAR_CONSOLE_ENABLED
            if (instance != null)
            {
                instance.UnregisterAllConsoleActions(target);
            }
#endif // LUNAR_CONSOLE_ENABLED
#endif // LUNAR_CONSOLE_FULL
#endif // LUNAR_CONSOLE_PLATFORM_SUPPORTED
        }

        /// <summary>
        ///     Sets console enabled or disabled.
        ///     Disabled console cannot be opened by user or API calls and does not collect logs.
        ///     Does nothing if platform is not supported or if plugin is not initialized.
        /// </summary>
        public static void SetConsoleEnabled(bool enabled)
        {
#if LUNAR_CONSOLE_PLATFORM_SUPPORTED
#if LUNAR_CONSOLE_FULL
#if LUNAR_CONSOLE_ENABLED
            if (instance != null)
            {
                instance.SetConsoleInstanceEnabled(enabled);
            }
#endif // LUNAR_CONSOLE_ENABLED
#endif // LUNAR_CONSOLE_FULL
#endif // LUNAR_CONSOLE_PLATFORM_SUPPORTED
        }

        /// <summary>
        ///     Force variables to be written to a file
        /// </summary>
        public void MarkVariablesDirty()
        {
            m_variablesDirty = true;
        }

        /// <summary>
        ///     Callback method to be called when the console is opened. You can pause your game here.
        /// </summary>
        public static Action onConsoleOpened { get; set; }

        /// <summary>
        ///     Callback method to be called when the console is closed. You can un-pause your game here.
        /// </summary>
        public static Action onConsoleClosed { get; set; }

#if LUNAR_CONSOLE_ENABLED

        private void ShowConsole()
        {
            if (m_platform != null)
            {
                m_platform.ShowConsole();
            }
        }

        private void HideConsole()
        {
            if (m_platform != null)
            {
                m_platform.HideConsole();
            }
        }

        private void ClearConsole()
        {
            if (m_platform != null)
            {
                m_platform.ClearConsole();
            }
        }

        private void RegisterConsoleAction(string name, Action actionDelegate)
        {
            if (registry != null)
            {
                registry.RegisterAction(name, actionDelegate);
            }
            else
            {
                Log.w("Can't register action '{0}': registry is not property initialized", name);
            }
        }

        private void UnregisterConsoleAction(Action actionDelegate)
        {
            if (registry != null)
            {
                registry.Unregister(actionDelegate);
            }
            else
            {
                Log.w("Can't unregister action '{0}': registry is not property initialized", actionDelegate);
            }
        }

        private void UnregisterConsoleAction(string name)
        {
            if (registry != null)
            {
                registry.Unregister(name);
            }
            else
            {
                Log.w("Can't unregister action '{0}': registry is not property initialized", name);
            }
        }

        private void UnregisterAllConsoleActions(object target)
        {
            if (registry != null)
            {
                registry.UnregisterAll(target);
            }
            else
            {
                Log.w("Can't unregister actions for target '{0}': registry is not property initialized", target);
            }
        }

        private void SetConsoleInstanceEnabled(bool enabled)
        {
            this.enabled = enabled;
        }

#endif // LUNAR_CONSOLE_ENABLED

        public static bool isConsoleEnabled
        {
            get
            {
#if LUNAR_CONSOLE_ENABLED
                return instance != null;
#else
                return false;
#endif
            }
        }

        public static LunarConsole instance { get; private set; }

        public CRegistry registry { get; private set; }

        #endregion
    }
}

namespace LunarConsolePluginInternal
{
    public static class LunarConsoleConfig
    {
        public static bool consoleEnabled;
        public static readonly bool consoleSupported;
        public static readonly bool freeVersion;
        public static readonly bool fullVersion;

        static LunarConsoleConfig()
        {
#if LUNAR_CONSOLE_ENABLED
            consoleEnabled = true;
#else
            consoleEnabled = false;
#endif

#if LUNAR_CONSOLE_PLATFORM_SUPPORTED
            consoleSupported = true;
#else
            consoleSupported = false;
#endif

#if LUNAR_CONSOLE_FULL
            freeVersion = false;
            fullVersion = true;
#else
            freeVersion = true;
            fullVersion = false;
#endif
        }

        public static bool actionsEnabled
        {
            get
            {
                if (consoleSupported && consoleEnabled)
                {
#if UNITY_EDITOR
                    return true;
#elif UNITY_IOS || UNITY_IPHONE
                    return Application.platform == RuntimePlatform.IPhonePlayer;
#elif UNITY_ANDROID
                    return Application.platform == RuntimePlatform.Android;
#endif
                }

                return false;
            }
        }
    }

#if UNITY_EDITOR

    public static class LunarConsolePluginEditorHelper
    {
#if LUNAR_CONSOLE_FREE
        [UnityEditor.MenuItem("Window/Lunar Mobile Console/Get PRO version...")]
        static void GetProVersion()
        {
            Application.OpenURL("https://goo.gl/aJbTsx");
        }
#endif

        public static string ResolvePluginFile()
        {
            try
            {
                string currentFile = new StackTrace(true).GetFrame(0).GetFileName();
                if (currentFile != null && File.Exists(currentFile))
                {
                    return currentFile;
                }
            }
            catch (Exception e)
            {
                Log.e(e, "Exception while resolving plugin files location");
            }

            return null;
        }
    }

#endif // UNITY_EDITOR

#pragma warning disable 0618

    /// <summary>
    ///     Class for collecting anonymous usage statistics
    /// </summary>
    public static class LunarConsoleAnalytics
    {
        public const int kUndefinedValue = int.MinValue;
        public static readonly string TrackingURL = "https://www.google-analytics.com/collect";

        public static string CreatePayload(string category, string action, int value)
        {
#if LUNAR_CONSOLE_ANALYTICS_ENABLED
            var payload = new StringBuilder(DefaultPayload);
            payload.AppendFormat("&ec={0}", WWW.EscapeURL(category));
            payload.AppendFormat("&ea={0}", WWW.EscapeURL(action));
            if (value != kUndefinedValue)
            {
                payload.AppendFormat("&ev={0}", value.ToString());
            }

            return payload.ToString();
#else
            return null;
#endif // LUNAR_CONSOLE_ANALYTICS_ENABLED
        }

#if LUNAR_CONSOLE_ANALYTICS_ENABLED

        private static readonly string DefaultPayload;

        static LunarConsoleAnalytics()
        {
            // tracking id
#if LUNAR_CONSOLE_FULL
            var trackingId = "UA-91768505-1";
#else
            var trackingId = "UA-91747018-1";
#endif

            var payload = new StringBuilder("v=1&t=event");
            payload.AppendFormat("&tid={0}", trackingId);
            payload.AppendFormat("&cid={0}", WWW.EscapeURL(SystemInfo.deviceUniqueIdentifier));
            payload.AppendFormat("&ua={0}", WWW.EscapeURL(SystemInfo.operatingSystem));
            payload.AppendFormat("&av={0}", WWW.EscapeURL(Constants.Version));
#if UNITY_EDITOR
            payload.AppendFormat("&ds={0}", "editor");
#else
            payload.AppendFormat("&ds={0}", "player");
#endif

            if (!string.IsNullOrEmpty(Application.productName))
            {
                string productName = WWW.EscapeURL(Application.productName);
                if (productName.Length <= 100)
                {
                    payload.AppendFormat("&an={0}", productName);
                }
            }

#if UNITY_5_6_OR_NEWER
            string identifier = Application.identifier;
#else
            var identifier = Application.bundleIdentifier;
#endif
            if (!string.IsNullOrEmpty(identifier))
            {
                string bundleIdentifier = WWW.EscapeURL(identifier);
                if (bundleIdentifier.Length <= 150)
                {
                    payload.AppendFormat("&aid={0}", bundleIdentifier);
                }
            }

            if (!string.IsNullOrEmpty(Application.companyName))
            {
                string companyName = WWW.EscapeURL(Application.companyName);
                if (companyName.Length <= 150)
                {
                    payload.AppendFormat("&aiid={0}", companyName);
                }
            }

            DefaultPayload = payload.ToString();
        }

        internal static IEnumerator TrackEvent(string category, string action, int value = kUndefinedValue)
        {
            string payload = CreatePayload(category, action, value);
            var www = new WWW(TrackingURL, Encoding.UTF8.GetBytes(payload));
            yield return www;
        }

#endif // LUNAR_CONSOLE_ANALYTICS_ENABLED
    }

#pragma warning restore 0618
}