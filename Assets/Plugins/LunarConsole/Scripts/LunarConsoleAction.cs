//
//  LunarConsoleAction.cs
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


using System;
using System.Collections.Generic;
using System.Reflection;
using LunarConsolePlugin;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LunarConsolePluginInternal
{
    public enum LunarPersistentListenerMode
    {
        Void,
        Bool,
        Int,
        Float,
        String,
        Object
    }

    [Serializable]
    internal class LunarArgumentCache : ISerializationCallbackReceiver
    {
        [SerializeField] private Object m_objectArgument;

        [SerializeField] private string m_objectArgumentAssemblyTypeName;

        [SerializeField] private int m_intArgument;

        [SerializeField] private float m_floatArgument;

        [SerializeField] private string m_stringArgument;

        [SerializeField] private bool m_boolArgument;

        public Object unityObjectArgument
        {
            get => m_objectArgument;
            set
            {
                m_objectArgument = value;
                m_objectArgumentAssemblyTypeName = !(value != null) ? string.Empty : value.GetType().AssemblyQualifiedName;
            }
        }

        public string unityObjectArgumentAssemblyTypeName => m_objectArgumentAssemblyTypeName;

        public int intArgument
        {
            get => m_intArgument;
            set => m_intArgument = value;
        }

        public float floatArgument
        {
            get => m_floatArgument;
            set => m_floatArgument = value;
        }

        public string stringArgument
        {
            get => m_stringArgument;
            set => m_stringArgument = value;
        }

        public bool boolArgument
        {
            get => m_boolArgument;
            set => m_boolArgument = value;
        }

        public void OnBeforeSerialize()
        {
            TidyAssemblyTypeName();
        }

        public void OnAfterDeserialize()
        {
            TidyAssemblyTypeName();
        }

        private void TidyAssemblyTypeName()
        {
            if (!string.IsNullOrEmpty(m_objectArgumentAssemblyTypeName))
            {
                var num = 2147483647;
                int num2 = m_objectArgumentAssemblyTypeName.IndexOf(", Version=");
                if (num2 != -1)
                {
                    num = Math.Min(num2, num);
                }

                num2 = m_objectArgumentAssemblyTypeName.IndexOf(", Culture=");
                if (num2 != -1)
                {
                    num = Math.Min(num2, num);
                }

                num2 = m_objectArgumentAssemblyTypeName.IndexOf(", PublicKeyToken=");
                if (num2 != -1)
                {
                    num = Math.Min(num2, num);
                }

                if (num != 2147483647)
                {
                    m_objectArgumentAssemblyTypeName = m_objectArgumentAssemblyTypeName.Substring(0, num);
                }
            }
        }
    }

    [Serializable]
    public class LunarConsoleActionCall
    {
        private static readonly Type[] kParamTypes =
        {
            typeof(int),
            typeof(float),
            typeof(string),
            typeof(bool)
        };

        public Object target => m_target;

        public string methodName => m_methodName;

        public LunarPersistentListenerMode mode => m_mode;

        public void Invoke()
        {
            MethodInfo method = null;
            object[] invokeParams = null;
            switch (m_mode)
            {
                case LunarPersistentListenerMode.Void:
                    method = ResolveMethod(m_target, m_methodName, typeof(void));
                    invokeParams = new object[0];
                    break;

                case LunarPersistentListenerMode.Bool:
                    method = ResolveMethod(m_target, m_methodName, typeof(bool));
                    invokeParams = new object[] { m_arguments.boolArgument };
                    break;

                case LunarPersistentListenerMode.Float:
                    method = ResolveMethod(m_target, m_methodName, typeof(float));
                    invokeParams = new object[] { m_arguments.floatArgument };
                    break;

                case LunarPersistentListenerMode.Int:
                    method = ResolveMethod(m_target, m_methodName, typeof(int));
                    invokeParams = new object[] { m_arguments.intArgument };
                    break;

                case LunarPersistentListenerMode.String:
                    method = ResolveMethod(m_target, m_methodName, typeof(string));
                    invokeParams = new object[] { m_arguments.stringArgument };
                    break;

                case LunarPersistentListenerMode.Object:
                    method = ResolveMethod(m_target, m_methodName, typeof(Object));
                    invokeParams = new object[] { m_arguments.unityObjectArgument };
                    break;

                default:
                    Log.e("Unable to invoke action: unexpected invoke mode '{0}'", m_mode);
                    return;
            }

            if (method != null)
            {
                method.Invoke(m_target, invokeParams);
            }
            else
            {
                Log.e("Unable to invoke action: can't resolve method '{0}'", m_methodName);
            }
        }

        private static MethodInfo ResolveMethod(object target, string methodName, Type paramType)
        {
            List<MethodInfo> methods = ClassUtils.ListInstanceMethods(target.GetType(), delegate(MethodInfo method)
            {
                if (method.Name != methodName)
                {
                    return false;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (paramType == typeof(void))
                {
                    return parameters.Length == 0;
                }

                return parameters.Length == 1 && (parameters[0].ParameterType == paramType || parameters[0].ParameterType.IsSubclassOf(paramType));
            });
            return methods.Count == 1 ? methods[0] : null;
        }

        public static bool IsPersistantListenerValid(Object target, string methodName, LunarPersistentListenerMode mode)
        {
            if (target == null)
            {
                return false;
            }

            List<MethodInfo> methods = ListActionMethods(target);
            foreach (MethodInfo method in methods)
            {
                if (method.Name == methodName)
                {
                    ParameterInfo[] parameters = method.GetParameters();
                    if (mode == LunarPersistentListenerMode.Void)
                    {
                        if (parameters.Length == 0)
                        {
                            return true;
                        }
                    }
                    else if (parameters.Length == 1)
                    {
                        Type paramType = parameters[0].ParameterType;
                        if (mode == LunarPersistentListenerMode.Bool && paramType == typeof(bool))
                        {
                            return true;
                        }

                        if (mode == LunarPersistentListenerMode.Float && paramType == typeof(float))
                        {
                            return true;
                        }

                        if (mode == LunarPersistentListenerMode.Int && paramType == typeof(int))
                        {
                            return true;
                        }

                        if (mode == LunarPersistentListenerMode.String && paramType == typeof(string))
                        {
                            return true;
                        }

                        if (mode == LunarPersistentListenerMode.Object && paramType.IsSubclassOf(typeof(Object)))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public static List<MethodInfo> ListActionMethods(object target)
        {
            var methods = new List<MethodInfo>();
            ClassUtils.ListMethods(methods, target.GetType(), IsValidActionMethod,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            return methods;
        }

        /// <summary>
        ///     Determines if method is a valid action delegate
        /// </summary>
        private static bool IsValidActionMethod(MethodInfo method)
        {
            if (!method.IsPublic)
            {
                return false; // only list public methods
            }

            if (method.ReturnType != typeof(void))
            {
                return false; // non-void return type are not allowed
            }

            if (method.IsAbstract)
            {
                return false; // don't list abstract methods
            }

            ParameterInfo[] methodParams = method.GetParameters();
            if (methodParams.Length > 1)
            {
                return false; // no more then a single param
            }

            object[] attributes = method.GetCustomAttributes(typeof(ObsoleteAttribute), false);
            if (attributes != null && attributes.Length > 0)
            {
                return false; // no obsolete methods
            }

            if (methodParams.Length == 1)
            {
                Type paramType = methodParams[0].ParameterType;
                if (!paramType.IsSubclassOf(typeof(Object)) && Array.IndexOf(kParamTypes, paramType) == -1)
                {
                    return false;
                }
            }

            return true;
        }

#pragma warning disable 0649

        [SerializeField] private Object m_target;

        [SerializeField] private string m_methodName;

        [SerializeField] private LunarPersistentListenerMode m_mode;

        [SerializeField] private LunarArgumentCache m_arguments;

#pragma warning restore 0649
    }

    public class LunarConsoleAction : MonoBehaviour
    {
        public List<LunarConsoleActionCall> calls => m_calls;

        private bool actionsEnabled => LunarConsoleConfig.actionsEnabled;

        private void Awake()
        {
            if (!actionsEnabled)
            {
                Destroy(this);
            }
        }

        private void Start()
        {
            if (actionsEnabled)
            {
                RegisterAction();
            }
            else
            {
                Destroy(this);
            }
        }

        private void OnDestroy()
        {
            if (actionsEnabled)
            {
                UnregisterAction();
            }
        }

        private void OnValidate()
        {
            if (m_calls != null && m_calls.Count > 0)
            {
                foreach (LunarConsoleActionCall call in m_calls)
                {
                    Validate(call);
                }
            }
        }

        private void Validate(LunarConsoleActionCall call)
        {
            if (call.target == null)
            {
                Debug.LogWarning(string.Format("Action '{0}' ({1}) is missing a target object", m_title, gameObject.name), gameObject);
            }
            else if (!LunarConsoleActionCall.IsPersistantListenerValid(call.target, call.methodName, call.mode))
            {
                Debug.LogWarning(
                    string.Format("Action '{0}' ({1}) is missing a handler <{2}.{3} ({4})>", m_title, gameObject.name, call.target.GetType(),
                        call.methodName, ModeParamTypeName(call.mode)), gameObject);
            }
        }

        private void RegisterAction()
        {
            LunarConsole.RegisterAction(m_title, InvokeAction);
        }

        private void UnregisterAction()
        {
            LunarConsole.UnregisterAction(InvokeAction);
        }

        private void InvokeAction()
        {
            if (m_calls != null && m_calls.Count > 0)
            {
                foreach (LunarConsoleActionCall call in m_calls)
                {
                    call.Invoke();
                }
            }
            else
            {
                Debug.LogWarningFormat("Action '{0}' has 0 calls", m_title);
            }
        }

        private static string ModeParamTypeName(LunarPersistentListenerMode mode)
        {
            switch (mode)
            {
                case LunarPersistentListenerMode.Void:
                    return "";

                case LunarPersistentListenerMode.Bool:
                    return "bool";

                case LunarPersistentListenerMode.Float:
                    return "float";

                case LunarPersistentListenerMode.Int:
                    return "int";

                case LunarPersistentListenerMode.String:
                    return "string";

                case LunarPersistentListenerMode.Object:
                    return "UnityEngine.Object";
            }

            return "???";
        }
#pragma warning disable 0649

        [SerializeField] private string m_title = "Untitled Action";

        [SerializeField] [HideInInspector] private List<LunarConsoleActionCall> m_calls;

#pragma warning restore 0649
    }
}