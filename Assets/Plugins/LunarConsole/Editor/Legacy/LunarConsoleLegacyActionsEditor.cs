//
//  LunarConsoleLegacyActionsEditor.cs
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
using LunarConsolePluginInternal;
using UnityEditor;
using UnityEngine;

namespace LunarConsoleEditorInternal
{
#pragma warning disable 0618
    [CustomEditor(typeof(LunarConsoleLegacyActions))]
#pragma warning restore 0618
    internal class LunarConsoleLegacyActionsEditor : Editor
    {
        private const string kPropActions = "m_actions";
        private const string kPropActionsSize = kPropActions + ".Array.size";

        private static readonly string[] kIgnoredMethods =
        {
            "Awake",
            "FixedUpdate",
            "LateUpdate",
            "OnAnimatorIK",
            "OnAnimatorMove",
            "OnApplicationFocus",
            "OnApplicationPause",
            "OnApplicationQuit",
            "OnAudioFilterRead",
            "OnBecameInvisible",
            "OnBecameVisible",
            "OnCollisionEnter",
            "OnCollisionEnter2D",
            "OnCollisionExit",
            "OnCollisionExit2D",
            "OnCollisionStay",
            "OnCollisionStay2D",
            "OnConnectedToServer",
            "OnControllerColliderHit",
            "OnDestroy",
            "OnDisable",
            "OnDisconnectedFromServer",
            "OnDrawGizmos",
            "OnDrawGizmosSelected",
            "OnEnable",
            "OnFailedToConnect",
            "OnFailedToConnectToMasterServer",
            "OnGUI",
            "OnJointBreak",
            "OnJointBreak2D",
            "OnMasterServerEvent",
            "OnMouseDown",
            "OnMouseDrag",
            "OnMouseEnter",
            "OnMouseExit",
            "OnMouseOver",
            "OnMouseUp",
            "OnMouseUpAsButton",
            "OnNetworkInstantiate",
            "OnParticleCollision",
            "OnParticleTrigger",
            "OnPlayerConnected",
            "OnPlayerDisconnected",
            "OnPostRender",
            "OnPreCull",
            "OnPreRender",
            "OnRenderImage",
            "OnRenderObject",
            "OnSerializeNetworkView",
            "OnServerInitialized",
            "OnTransformChildrenChanged",
            "OnTransformParentChanged",
            "OnTriggerEnter",
            "OnTriggerEnter2D",
            "OnTriggerExit",
            "OnTriggerExit2D",
            "OnTriggerStay",
            "OnTriggerStay2D",
            "OnValidate",
            "OnWillRenderObject",
            "Reset",
            "Start",
            "Update"
        };

        private SerializedProperty m_actionsCountProperty;
        private SerializedProperty m_actionsPropertry;

        private int m_selectedIndex = -1;

        private void OnEnable()
        {
            m_actionsPropertry = serializedObject.FindProperty(kPropActions);
            m_actionsCountProperty = serializedObject.FindProperty(kPropActionsSize);
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            const float height = 45;

            EditorGUILayout.Space();
            Rect selectedRect = GUILayoutUtility.GetLastRect();
            selectedRect.y += selectedRect.height;
            selectedRect.height = height + EditorGUIUtility.standardVerticalSpacing;

            serializedObject.Update();

            EditorGUILayout.BeginVertical();
            {
                for (var i = 0; i < m_actionsPropertry.arraySize; ++i)
                {
                    if (i == m_selectedIndex)
                    {
                        EditorGUI.DrawRect(selectedRect, GUI.skin.settings.selectionColor);
                    }

                    EditorGUILayout.BeginVertical(GUILayout.Height(height));
                    {
                        EditorGUI.BeginChangeCheck();

                        SerializedProperty nameProperty = serializedObject.FindProperty("m_actions.Array.data[" + i + "].m_name");
                        SerializedProperty targetProperty = serializedObject.FindProperty("m_actions.Array.data[" + i + "].m_target");
                        SerializedProperty componentTypeProperty =
                            serializedObject.FindProperty("m_actions.Array.data[" + i + "].m_componentTypeName");
                        SerializedProperty componentMethodProperty =
                            serializedObject.FindProperty("m_actions.Array.data[" + i + "].m_componentMethodName");

                        EditorGUILayout.PropertyField(nameProperty, new GUIContent("Display Name"));

                        EditorGUILayout.BeginHorizontal();
                        {
                            EditorGUILayout.PropertyField(targetProperty, GUIContent.none, GUILayout.Width(EditorGUIUtility.labelWidth));
                            var target = targetProperty.objectReferenceValue as GameObject;

                            Functions functions = ListFunctions(target);

                            int oldIndex = ResolveFuntionIndex(functions, componentTypeProperty.stringValue, componentMethodProperty.stringValue);
                            int newIndex = EditorGUILayout.Popup(oldIndex, functions.names);
                            if (oldIndex != newIndex)
                            {
                                if (newIndex >= 2)
                                {
                                    string typeName = functions.componentTypes[newIndex - 2].AssemblyQualifiedName;
                                    string methodName = functions.componentMethods[newIndex - 2].Name;

                                    componentTypeProperty.stringValue = typeName;
                                    componentMethodProperty.stringValue = methodName;
                                    nameProperty.stringValue = StringUtils.ToDisplayName(methodName);
                                }
                                else
                                {
                                    componentTypeProperty.stringValue = null;
                                    componentMethodProperty.stringValue = null;
                                    nameProperty.stringValue = "";
                                }
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndVertical();

                    if (Event.current.type == EventType.MouseDown)
                    {
                        if (selectedRect.Contains(Event.current.mousePosition))
                        {
                            m_selectedIndex = i;
                            Repaint();
                        }
                    }

                    selectedRect.y += selectedRect.height;
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add"))
            {
                m_actionsCountProperty.intValue++;
            }

            if (GUILayout.Button("Remove"))
            {
                if (m_selectedIndex >= 0 && m_selectedIndex < m_actionsCountProperty.intValue)
                {
                    m_actionsPropertry.DeleteArrayElementAtIndex(m_selectedIndex);
                }
            }

            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }

        private int ResolveFuntionIndex(Functions functions, string typeName, string methodName)
        {
            if (typeName == null || methodName == null)
            {
                return 0;
            }

            var type = Type.GetType(typeName);
            if (type == null)
            {
                return 0;
            }

            string functionName = type.Name + "/" + methodName;

            int index = Array.IndexOf(functions.names, functionName);
            return Mathf.Max(0, index);
        }

        private Functions ListFunctions(GameObject obj)
        {
            var functions = new List<string>();
            var componentTypes = new List<Type>();
            var componentMethods = new List<MethodInfo>();
            functions.Add("No Function");

            if (obj != null)
            {
                foreach (Component component in obj.GetComponents<Component>())
                {
                    Type type = component.GetType();
                    List<MethodInfo> methods = ClassUtils.ListInstanceMethods(type, delegate(MethodInfo method)
                    {
                        return Array.IndexOf(kIgnoredMethods, method.Name) == -1 && // not forbidden name
                               method.ReturnType == typeof(void) && // with no return type
                               method.GetParameters().Length == 0; // and no parameters
                    });

                    if (functions.Count == 1)
                    {
                        functions.Add("/");
                    }

                    foreach (MethodInfo method in methods)
                    {
                        functions.Add(type.Name + "/" + method.Name);
                        componentTypes.Add(type);
                        componentMethods.Add(method);
                    }
                }
            }

            Functions result;
            result.names = functions.ToArray();
            result.componentTypes = componentTypes.ToArray();
            result.componentMethods = componentMethods.ToArray();

            return result;
        }

        private struct Functions
        {
            public string[] names;
            public Type[] componentTypes;
            public MethodInfo[] componentMethods;
        }
    }
}