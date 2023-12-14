//-----------------------------------------------------------------------
// <copyright file="LocalizedStringFormatter.cs" company="Sirenix IVS">
// Copyright (c) Sirenix IVS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Reflection;
using Sirenix.OdinInspector.Modules.Localization;
using Sirenix.Serialization;
using UnityEngine.Localization;

[assembly: RegisterFormatter(typeof(LocalizedStringFormatter))]

namespace Sirenix.OdinInspector.Modules.Localization
{
    public class LocalizedStringFormatter : ReflectionOrEmittedBaseFormatter<LocalizedString>
    {
        private static readonly FieldInfo m_LocalVariables_Field;

        static LocalizedStringFormatter()
        {
            m_LocalVariables_Field =
                typeof(LocalizedString).GetField("m_LocalVariables", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (m_LocalVariables_Field == null)
            {
                DefaultLoggers.DefaultLogger.LogError("Could not find field 'UnityEngine.LocalizedString.m_LocalVariables'" +
                                                      " - the internals of the Localization package have changed, and deserialization of Odin-serialized" +
                                                      " LocalizedString instances may be broken in some cases.");
            }
        }

        protected override LocalizedString GetUninitializedObject()
        {
            return new LocalizedString();
        }

        protected override void DeserializeImplementation(ref LocalizedString value, IDataReader reader)
        {
            base.DeserializeImplementation(ref value, reader);

            if (m_LocalVariables_Field != null && value != null)
            {
                object localVariablesList = m_LocalVariables_Field.GetValue(value);

                // This list is not allowed to be null!
                if (localVariablesList == null)
                {
                    localVariablesList = Activator.CreateInstance(m_LocalVariables_Field.FieldType);
                    m_LocalVariables_Field.SetValue(value, localVariablesList);
                }
            }
        }
    }
}