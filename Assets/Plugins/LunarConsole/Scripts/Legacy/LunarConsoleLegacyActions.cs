//
//  LunarConsoleLegacyActions.cs
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


﻿using System;
using System.Collections.Generic;
using System.Reflection;
using LunarConsolePlugin;
using UnityEngine;

namespace LunarConsolePluginInternal
{
    [Serializable]
    public class LunarConsoleLegacyAction
    {
        public void Register()
        {
            if (string.IsNullOrEmpty(m_name))
            {
                Log.w("Unable to register action: name is null or empty");
            }
            else if (m_target == null)
            {
                Log.w("Unable to register action '{0}': target GameObject is missing", m_name);
            }
            else if (string.IsNullOrEmpty(m_componentMethodName))
            {
                Log.w("Unable to register action '{0}' for '{1}': function is missing", m_name, m_target.name);
            }
            else
            {
                LunarConsole.RegisterAction(m_name, Invoke);
            }
        }

        public void Unregister()
        {
            LunarConsole.UnregisterAction(Invoke);
        }

        private void Invoke()
        {
            if (m_target == null)
            {
                Log.e("Can't invoke action '{0}': target is not set", m_name);
                return;
            }

            if (m_componentTypeName == null)
            {
                Log.e("Can't invoke action '{0}': method is not set", m_name);
                return;
            }

            if (m_componentMethodName == null)
            {
                Log.e("Can't invoke action '{0}': method is not set", m_name);
                return;
            }

            if (m_componentType == null || m_componentMethod == null)
            {
                if (!ResolveInvocation())
                {
                    return;
                }
            }

            Component component = m_target.GetComponent(m_componentType);
            if (component == null)
            {
                Log.w("Missing component {0}", m_componentType);
                return;
            }

            try
            {
                m_componentMethod.Invoke(component, kEmptyArgs);
            }
            catch (TargetInvocationException e)
            {
                Log.e(e.InnerException, "Exception while invoking action '{0}'", m_name);
            }
            catch (Exception e)
            {
                Log.e(e, "Exception while invoking action '{0}'", m_name);
            }
        }

        private bool ResolveInvocation()
        {
            try
            {
                m_componentType = Type.GetType(m_componentTypeName);
                if (m_componentType == null)
                {
                    Log.w("Can't resolve type {0}", m_componentTypeName);
                    return false;
                }

                m_componentMethod =
                    m_componentType.GetMethod(m_componentMethodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (m_componentMethod == null)
                {
                    Log.w("Can't resolve method {0} of type {1}", m_componentMethod, m_componentType);
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                Log.e(e);
                return false;
            }
        }

        public void Validate()
        {
            if (string.IsNullOrEmpty(m_name))
            {
                Log.w("Missing action name");
            }

            if (m_target == null)
            {
                Log.w("Missing action target");
            }

            if (m_componentType != null && m_componentMethodName != null)
            {
                ResolveInvocation();
            }
        }
#pragma warning disable 0649

        private static readonly object[] kEmptyArgs = { };

        [SerializeField] private string m_name;

        [SerializeField] private GameObject m_target;

        [SerializeField] private string m_componentTypeName;

        [SerializeField] private string m_componentMethodName;

        private Type m_componentType;
        private MethodInfo m_componentMethod;

#pragma warning restore 0649
    }

    [Obsolete("Use 'Lunar Console Action' instead")]
    public class LunarConsoleLegacyActions : MonoBehaviour
    {
        public List<LunarConsoleLegacyAction> actions => m_actions;

        private bool actionsEnabled => LunarConsoleConfig.actionsEnabled;

        private void Awake()
        {
            if (!actionsEnabled)
            {
                Destroy(this);
            }

            if (m_dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void Start()
        {
            if (actionsEnabled)
            {
                foreach (LunarConsoleLegacyAction action in m_actions)
                {
                    action.Register();
                }
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
                foreach (LunarConsoleLegacyAction action in m_actions)
                {
                    action.Unregister();
                }
            }
        }

        public void AddAction(LunarConsoleLegacyAction action)
        {
            m_actions.Add(action);
        }
#pragma warning disable 0649

        [SerializeField] private bool m_dontDestroyOnLoad;

        [SerializeField] [HideInInspector] private List<LunarConsoleLegacyAction> m_actions;

#pragma warning restore 0649
    }
}