//
//  CRegistry.cs
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
using LunarConsolePlugin;

namespace LunarConsolePluginInternal
{
    internal delegate bool CActionFilter(CAction action);

    public interface ICRegistryDelegate
    {
        void OnActionRegistered(CRegistry registry, CAction action);
        void OnActionUnregistered(CRegistry registry, CAction action);
        void OnVariableRegistered(CRegistry registry, CVar cvar);
        void OnVariableUpdated(CRegistry registry, CVar cvar);
    }

    public class CRegistry
    {
        #region Destroyable

        public void Destroy()
        {
            actions.Clear();
            cvars.Clear();
            registryDelegate = null;
        }

        #endregion

        #region Commands registry

        public CAction RegisterAction(string name, Delegate actionDelegate)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw new ArgumentException("Action's name is empty");
            }

            if (actionDelegate == null)
            {
                throw new ArgumentNullException("actionDelegate");
            }

            CAction action = actions.Find(name);
            if (action != null)
            {
                // Log.w("Overriding action: {0}", name);
                action.ActionDelegate = actionDelegate;
            }
            else
            {
                action = new CAction(name, actionDelegate);
                actions.Add(action);

                if (registryDelegate != null)
                {
                    registryDelegate.OnActionRegistered(this, action);
                }
            }

            return action;
        }

        public bool Unregister(string name)
        {
            return Unregister(delegate(CAction action) { return action.Name == name; });
        }

        public bool Unregister(int id)
        {
            return Unregister(delegate(CAction action) { return action.Id == id; });
        }

        public bool Unregister(Delegate del)
        {
            return Unregister(delegate(CAction action) { return action.ActionDelegate == del; });
        }

        public bool UnregisterAll(object target)
        {
            return target != null && Unregister(delegate(CAction action) { return action.ActionDelegate.Target == target; });
        }

        private bool Unregister(CActionFilter filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException("filter");
            }

            IList<CAction> actionsToRemove = new List<CAction>();
            foreach (CAction action in actions)
            {
                if (filter(action))
                {
                    actionsToRemove.Add(action);
                }
            }

            foreach (CAction action in actionsToRemove)
            {
                RemoveAction(action);
            }

            return actionsToRemove.Count > 0;
        }

        private bool RemoveAction(CAction action)
        {
            if (actions.Remove(action.Id))
            {
                if (registryDelegate != null)
                {
                    registryDelegate.OnActionUnregistered(this, action);
                }

                return true;
            }

            return false;
        }

        public CAction FindAction(int id)
        {
            return actions.Find(id);
        }

        #endregion

        #region Variables

        public void Register(CVar cvar)
        {
            cvars.Add(cvar);

            if (registryDelegate != null)
            {
                registryDelegate.OnVariableRegistered(this, cvar);
            }
        }

        public CVar FindVariable(int variableId)
        {
            return cvars.Find(variableId);
        }

        public CVar FindVariable(string variableName)
        {
            return cvars.Find(variableName);
        }

        #endregion

        #region Properties

        public ICRegistryDelegate registryDelegate { get; set; }

        public CActionList actions { get; } = new();

        public CVarList cvars { get; } = new();

        #endregion
    }
}