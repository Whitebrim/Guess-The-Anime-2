//
//  LunarConsoleActionEditor.cs
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
using System.Text;
using LunarConsolePluginInternal;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LunarConsoleEditorInternal
{
    [CustomEditor(typeof(LunarConsoleAction))]
    internal class LunarConsoleActionEditor : Editor
    {
        private const string kPropCalls = "m_calls";
        private const string kPropMode = "m_mode";
        private const string kPropTarget = "m_target";
        private const string kPropMethod = "m_methodName";
        private const string kPropArguments = "m_arguments";

        private const string kPropObjectArgumentAssemblyTypeName = "m_objectArgumentAssemblyTypeName";
        private ReorderableList list;

        private void OnEnable()
        {
            list = new ReorderableList(serializedObject, serializedObject.FindProperty(kPropCalls), false, true, true, true);
            list.drawHeaderCallback = DrawListHeader;
            list.drawElementCallback = DrawListElement;
            list.elementHeight = 43;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            list.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawListHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "On Click ()");
        }

        private void DrawListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty arrayElementAtIndex = list.serializedProperty.GetArrayElementAtIndex(index);
            rect.y += 1f;
            Rect[] rowRects = GetRowRects(rect);
            Rect runtimeModeRect = rowRects[0];
            Rect targetRect = rowRects[1];
            Rect methodRect = rowRects[2];
            Rect argumentRect = rowRects[3];
            SerializedProperty modeProperty = arrayElementAtIndex.FindPropertyRelative(kPropMode);
            SerializedProperty targetProperty = arrayElementAtIndex.FindPropertyRelative(kPropTarget);
            SerializedProperty methodProperty = arrayElementAtIndex.FindPropertyRelative(kPropMethod);
            SerializedProperty argumentsProperty = arrayElementAtIndex.FindPropertyRelative(kPropArguments);

            Color backgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.white;

            bool oldFlag = GUI.enabled;
            GUI.enabled = false;
            GUI.Box(runtimeModeRect, "Runtime Only", EditorStyles.popup);
            GUI.enabled = oldFlag;

            EditorGUI.BeginChangeCheck();
            GUI.Box(targetRect, GUIContent.none);
            EditorGUI.PropertyField(targetRect, targetProperty, GUIContent.none);
            if (EditorGUI.EndChangeCheck())
            {
                methodProperty.stringValue = null;
            }

            var persistentListenerMode = (LunarPersistentListenerMode)modeProperty.enumValueIndex;
            if (targetProperty.objectReferenceValue == null || string.IsNullOrEmpty(methodProperty.stringValue))
            {
                persistentListenerMode = LunarPersistentListenerMode.Void;
            }

            SerializedProperty argumentProperty;
            switch (persistentListenerMode)
            {
                case LunarPersistentListenerMode.Object:
                    argumentProperty = argumentsProperty.FindPropertyRelative("m_objectArgument");
                    break;
                case LunarPersistentListenerMode.Int:
                    argumentProperty = argumentsProperty.FindPropertyRelative("m_intArgument");
                    break;
                case LunarPersistentListenerMode.Float:
                    argumentProperty = argumentsProperty.FindPropertyRelative("m_floatArgument");
                    break;
                case LunarPersistentListenerMode.String:
                    argumentProperty = argumentsProperty.FindPropertyRelative("m_stringArgument");
                    break;
                case LunarPersistentListenerMode.Bool:
                    argumentProperty = argumentsProperty.FindPropertyRelative("m_boolArgument");
                    break;
                default:
                    argumentProperty = argumentsProperty.FindPropertyRelative("m_intArgument");
                    break;
            }

            string argumentAssemblyTypeName = argumentsProperty.FindPropertyRelative(kPropObjectArgumentAssemblyTypeName).stringValue;
            Type argumentType = typeof(Object);
            if (!string.IsNullOrEmpty(argumentAssemblyTypeName))
            {
                argumentType = Type.GetType(argumentAssemblyTypeName, false) ?? typeof(Object);
            }

            if (persistentListenerMode == LunarPersistentListenerMode.Object)
            {
                EditorGUI.BeginChangeCheck();
                Object objectReferenceValue =
                    EditorGUI.ObjectField(argumentRect, GUIContent.none, argumentProperty.objectReferenceValue, argumentType, true);
                if (EditorGUI.EndChangeCheck())
                {
                    argumentProperty.objectReferenceValue = objectReferenceValue;
                }
            }
            else if (persistentListenerMode != LunarPersistentListenerMode.Void)
            {
                EditorGUI.PropertyField(argumentRect, argumentProperty, GUIContent.none);
            }

            using (new DisabledScopeCompat(targetProperty.objectReferenceValue == null))
            {
                EditorGUI.BeginProperty(methodRect, GUIContent.none, methodProperty);
                GUIContent content;
                {
                    var stringBuilder = new StringBuilder();
                    if (targetProperty.objectReferenceValue == null || string.IsNullOrEmpty(methodProperty.stringValue))
                    {
                        stringBuilder.Append("No Function");
                    }
                    else if (!LunarConsoleActionCall.IsPersistantListenerValid(targetProperty.objectReferenceValue, methodProperty.stringValue,
                                 persistentListenerMode))
                    {
                        var componentName = "UnknownComponent";
                        Object target = targetProperty.objectReferenceValue;
                        if (target != null)
                        {
                            componentName = target.GetType().Name;
                        }

                        stringBuilder.Append(string.Format("<Missing {0}.{1}>", componentName, methodProperty.stringValue));
                    }
                    else
                    {
                        stringBuilder.Append(targetProperty.objectReferenceValue.GetType().Name);
                        if (!string.IsNullOrEmpty(methodProperty.stringValue))
                        {
                            stringBuilder.Append(".");
                            if (methodProperty.stringValue.StartsWith("set_"))
                            {
                                stringBuilder.Append(methodProperty.stringValue.Substring(4));
                            }
                            else
                            {
                                stringBuilder.Append(methodProperty.stringValue);
                            }
                        }
                    }

                    content = new GUIContent(stringBuilder.ToString());
                }
                if (GUI.Button(methodRect, content, EditorStyles.popup))
                {
                    BuildPopupList(arrayElementAtIndex).DropDown(methodRect);
                }

                EditorGUI.EndProperty();
            }

            GUI.backgroundColor = backgroundColor;
        }

        private GenericMenu BuildPopupList(SerializedProperty serializedProperty)
        {
            SerializedProperty targetProperty = serializedProperty.FindPropertyRelative(kPropTarget);
            SerializedProperty methodProperty = serializedProperty.FindPropertyRelative(kPropMethod);

            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("No Function"), methodProperty.stringValue == null, delegate
            {
                methodProperty.stringValue = null;
                serializedObject.ApplyModifiedProperties();
            });

            Object target = targetProperty.objectReferenceValue;
            if (target != null)
            {
                menu.AddSeparator("/");

                Function[] functions = ListFunctions(target);
                foreach (Function function in functions)
                {
                    bool selected = target == function.target && methodProperty.stringValue == function.method.Name;
                    menu.AddItem(new GUIContent(function.target.GetType().Name + "/" + function.displayName), selected, delegate
                    {
                        targetProperty.objectReferenceValue = function.target;
                        methodProperty.stringValue = function.method.Name;
                        UpdateParamProperty(serializedProperty, function.paramType);
                        serializedObject.ApplyModifiedProperties();
                    });
                }
            }

            return menu;
        }

        private void UpdateParamProperty(SerializedProperty serializedProperty, Type paramType)
        {
            SerializedProperty modeProperty = serializedProperty.FindPropertyRelative(kPropMode);
            SerializedProperty argumentsProperty = serializedProperty.FindPropertyRelative(kPropArguments);
            SerializedProperty typeAssemblyProperty = argumentsProperty.FindPropertyRelative(kPropObjectArgumentAssemblyTypeName);

            var mode = LunarPersistentListenerMode.Void;
            if (paramType != null)
            {
                if (paramType.IsSubclassOf(typeof(Object)))
                {
                    mode = LunarPersistentListenerMode.Object;
                }
                else if (paramType == typeof(int))
                {
                    mode = LunarPersistentListenerMode.Int;
                }
                else if (paramType == typeof(float))
                {
                    mode = LunarPersistentListenerMode.Float;
                }
                else if (paramType == typeof(string))
                {
                    mode = LunarPersistentListenerMode.String;
                }
                else if (paramType == typeof(bool))
                {
                    mode = LunarPersistentListenerMode.Bool;
                }
                else
                {
                    Log.e("Unexpected param type: {0}", paramType);
                }
            }

            modeProperty.enumValueIndex = (int)mode;
            typeAssemblyProperty.stringValue = paramType != null ? paramType.AssemblyQualifiedName : null;
        }

        private Rect[] GetRowRects(Rect rect)
        {
            var array = new Rect[4];
            rect.height = 16f;
            rect.y += 2f;
            Rect rect2 = rect;
            rect2.width *= 0.3f;
            Rect rect3 = rect2;
            rect3.y += EditorGUIUtility.singleLineHeight + 2f;
            Rect rect4 = rect;
            rect4.xMin = rect3.xMax + 5f;
            Rect rect5 = rect4;
            rect5.y += EditorGUIUtility.singleLineHeight + 2f;
            array[0] = rect2;
            array[1] = rect3;
            array[2] = rect4;
            array[3] = rect5;
            return array;
        }

        private Function[] ListFunctions(Object obj)
        {
            if (obj is Component)
            {
                obj = ((Component)obj).gameObject;
            }

            var functions = new List<Function>();
            if (obj != null)
            {
                var targets = new List<Object>();
                targets.Add(obj);

                if (obj is GameObject)
                {
                    var gameObject = obj as GameObject;
                    foreach (Component component in gameObject.GetComponents<Component>())
                    {
                        targets.Add(component);
                    }
                }

                foreach (Object target in targets)
                {
                    List<MethodInfo> methods = LunarConsoleActionCall.ListActionMethods(target);
                    methods.Sort(delegate(MethodInfo a, MethodInfo b)
                    {
                        return a.IsSpecialName == b.IsSpecialName ? a.Name.CompareTo(b.Name) : a.IsSpecialName ? -1 : 1;
                    });

                    foreach (MethodInfo method in methods)
                    {
                        functions.Add(new Function(target, method));
                    }
                }
            }

            return functions.ToArray();
        }

        private struct Function
        {
            public readonly Object target;
            public readonly MethodInfo method;

            public Function(Object target, MethodInfo method)
            {
                this.target = target;
                this.method = method;
            }

            public bool isProperty => method.IsSpecialName && method.Name.StartsWith("set_");

            public string simpleName => isProperty ? method.Name.Substring("set_".Length) : method.Name;

            public Type paramType
            {
                get
                {
                    ParameterInfo[] methodParams = method.GetParameters();
                    return methodParams.Length > 0 ? methodParams[0].ParameterType : null;
                }
            }

            public string displayName
            {
                get
                {
                    Type functionParamType = paramType;
                    if (functionParamType != null)
                    {
                        string typeName = ClassUtils.TypeShortName(functionParamType);
                        return isProperty ? string.Format("{0} {1}", typeName, simpleName) : string.Format("{0} ({1})", simpleName, typeName);
                    }

                    return string.Format("{0} ()", simpleName);
                }
            }
        }
    }
}