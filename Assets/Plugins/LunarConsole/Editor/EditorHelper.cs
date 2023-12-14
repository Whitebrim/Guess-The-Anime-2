//
//  EditorHelper.cs
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
using UnityEditor;

namespace LunarConsoleEditorInternal
{
    public static class EditorHelper
    {
        public static void DelayCallOnce(Action action)
        {
            new EditorCallback(action).ScheduleDelayCall();
        }

        private sealed class EditorCallback
        {
            private readonly Action action;

            public EditorCallback(Action action)
            {
                this.action = action;
            }

            public void ScheduleDelayCall()
            {
                EditorApplication.delayCall += Invoke;
            }

            private void Invoke()
            {
                try
                {
                    action();
                }
                finally
                {
                    // ReSharper disable once DelegateSubtraction
                    EditorApplication.delayCall -= Invoke;
                }
            }
        }
    }
}