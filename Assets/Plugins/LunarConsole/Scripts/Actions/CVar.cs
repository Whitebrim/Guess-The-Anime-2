//
//  CVar.cs
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
using System.Collections;
using System.Collections.Generic;
using LunarConsolePluginInternal;

namespace LunarConsolePlugin
{
    public delegate void CVarChangedDelegate(CVar cvar);

    public enum CVarType
    {
        Boolean,
        Integer,
        Float,
        String,
        Enum
    }

    internal struct CValue
    {
        public string stringValue;
        public int intValue;
        public float floatValue;

        public bool Equals(ref CValue other)
        {
            return other.intValue == intValue &&
                   other.floatValue == floatValue &&
                   other.stringValue == stringValue;
        }
    }

    public struct CVarValueRange
    {
        public static readonly CVarValueRange Undefined = new(float.NaN, float.NaN);

        public readonly float min;
        public readonly float max;

        public CVarValueRange(float min, float max)
        {
            this.min = min;
            this.max = max;
        }

        public bool IsValid => !float.IsNaN(min) && !float.IsNaN(max);
    }

    [Flags]
    public enum CFlags
    {
        /// <summary>
        ///     No flags (default value)
        /// </summary>
        None = 0,

        /// <summary>
        ///     Won't be listed in UI
        /// </summary>
        Hidden = 1 << 1,

        /// <summary>
        ///     Don't save between sessions
        /// </summary>
        NoArchive = 1 << 2
    }

    public class CVar : IEquatable<CVar>, IComparable<CVar>
    {
        private static int s_nextId;

        private CValue m_defaultValue;

        private CVarChangedDelegateList m_delegateList;
        private CVarValueRange m_range = CVarValueRange.Undefined;

        private CValue m_value;

        public CVar(string name, bool defaultValue, CFlags flags = CFlags.None)
            : this(name, CVarType.Boolean, flags)
        {
            IntValue = defaultValue ? 1 : 0;
            m_defaultValue = m_value;
        }

        public CVar(string name, int defaultValue, CFlags flags = CFlags.None)
            : this(name, CVarType.Integer, flags)
        {
            IntValue = defaultValue;
            m_defaultValue = m_value;
        }

        public CVar(string name, float defaultValue, CFlags flags = CFlags.None)
            : this(name, CVarType.Float, flags)
        {
            FloatValue = defaultValue;
            m_defaultValue = m_value;
        }

        public CVar(string name, string defaultValue, CFlags flags = CFlags.None)
            : this(name, CVarType.String, flags)
        {
            Value = defaultValue;
            m_defaultValue = m_value;
        }

        protected CVar(string name, CVarType type, CFlags flags)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            Id = ++s_nextId;

            Name = name;
            Type = type;
            Flags = flags;
        }

        #region IComparable

        public int CompareTo(CVar other)
        {
            return Name.CompareTo(other.Name);
        }

        #endregion

        #region IEquatable

        public bool Equals(CVar other)
        {
            return other != null &&
                   other.Name == Name &&
                   other.m_value.Equals(ref m_value) &&
                   other.m_defaultValue.Equals(ref m_defaultValue) &&
                   other.Type == Type;
        }

        #endregion

        #region Delegates

        public void AddDelegate(CVarChangedDelegate del)
        {
            if (del == null)
            {
                throw new ArgumentNullException("del");
            }

            if (m_delegateList == null)
            {
                m_delegateList = new CVarChangedDelegateList(1);
                m_delegateList.Add(del);
            }
            else if (!m_delegateList.Contains(del))
            {
                m_delegateList.Add(del);
            }
        }

        public void RemoveDelegate(CVarChangedDelegate del)
        {
            if (del != null && m_delegateList != null)
            {
                m_delegateList.Remove(del);

                if (m_delegateList.Count == 0)
                {
                    m_delegateList = null;
                }
            }
        }

        public void RemoveDelegates(object target)
        {
            if (target != null && m_delegateList != null)
            {
                for (int i = m_delegateList.Count - 1; i >= 0; --i)
                {
                    if (m_delegateList.Get(i).Target == target)
                    {
                        m_delegateList.RemoveAt(i);
                    }
                }

                if (m_delegateList.Count == 0)
                {
                    m_delegateList = null;
                }
            }
        }

        private void NotifyValueChanged()
        {
            if (m_delegateList != null && m_delegateList.Count > 0)
            {
                m_delegateList.NotifyValueChanged(this);
            }
        }

        #endregion

        #region Properties

        public int Id { get; }

        public string Name { get; }

        public CVarType Type { get; }

        public string DefaultValue
        {
            get => m_defaultValue.stringValue;
            protected set => m_defaultValue.stringValue = value;
        }

        public bool IsString => Type == CVarType.String;

        public string Value
        {
            get => m_value.stringValue;
            set
            {
                bool changed = m_value.stringValue != value;

                m_value.stringValue = value;
                m_value.floatValue = IsInt || IsFloat ? StringUtils.ParseFloat(value, 0.0f) : 0.0f;
                m_value.intValue = IsInt || IsFloat ? (int)FloatValue : 0;

                if (changed)
                {
                    NotifyValueChanged();
                }
            }
        }

        public CVarValueRange Range
        {
            get => m_range;
            set => m_range = value;
        }

        public bool HasRange => m_range.IsValid;

        public bool IsInt => Type == CVarType.Integer || Type == CVarType.Boolean;

        public int IntValue
        {
            get => m_value.intValue;
            set
            {
                bool changed = m_value.intValue != value;

                m_value.stringValue = StringUtils.ToString(value);
                m_value.intValue = value;
                m_value.floatValue = value;

                if (changed)
                {
                    NotifyValueChanged();
                }
            }
        }

        public bool IsFloat => Type == CVarType.Float;

        public float FloatValue
        {
            get => m_value.floatValue;
            set
            {
                float oldValue = m_value.floatValue;

                m_value.stringValue = StringUtils.ToString(value);
                m_value.intValue = (int)value;
                m_value.floatValue = value;

                if (oldValue != value)
                {
                    NotifyValueChanged();
                }
            }
        }

        public bool IsBool => Type == CVarType.Boolean;

        public bool BoolValue
        {
            get => m_value.intValue != 0;
            set => IntValue = value ? 1 : 0;
        }

        public virtual string[] AvailableValues => null;

        public bool IsDefault
        {
            get => m_value.Equals(m_defaultValue);
            set
            {
                bool changed = IsDefault ^ value;
                m_value = m_defaultValue;

                if (changed)
                {
                    NotifyValueChanged();
                }
            }
        }

        public bool HasFlag(CFlags flag)
        {
            return (Flags & flag) != 0;
        }

        public CFlags Flags { get; }

        public bool IsHidden => (Flags & CFlags.Hidden) != 0;

        #endregion

        #region Operators

        public static implicit operator string(CVar cvar)
        {
            return cvar.m_value.stringValue;
        }

        public static implicit operator int(CVar cvar)
        {
            return cvar.m_value.intValue;
        }

        public static implicit operator float(CVar cvar)
        {
            return cvar.m_value.floatValue;
        }

        public static implicit operator bool(CVar cvar)
        {
            return cvar.m_value.intValue != 0;
        }

        #endregion
    }

    public class CEnumVar<T> : CVar where T : struct, IConvertible
    {
        private readonly string[] m_names;
        private readonly IDictionary<string, T> m_valueLookup;

        public CEnumVar(string name, T defaultValue, CFlags flags = CFlags.None) : base(name, CVarType.Enum, flags)
        {
            if (!typeof(T).IsEnum)
            {
                throw new ArgumentException("T must be an enumerated type");
            }

            var value = defaultValue.ToString();

            Value = value;
            DefaultValue = value;

            Array values = Enum.GetValues(typeof(T));
            m_names = Enum.GetNames(typeof(T));

            m_valueLookup = new Dictionary<string, T>();
            for (var i = 0; i < values.Length; i++)
            {
                m_valueLookup[m_names[i]] = (T)values.GetValue(i);
            }
        }

        public override string[] AvailableValues => m_names;

        public T EnumValue => m_valueLookup[Value];

#if UNITY_2017_1_OR_NEWER

        public static implicit operator T(CEnumVar<T> cvar)
        {
            return cvar.EnumValue;
        }

#endif
    }

    public class CVarList : IEnumerable<CVar>
    {
        private readonly Dictionary<int, CVar> m_lookupById;
        private readonly List<CVar> m_variables;

        public CVarList()
        {
            m_variables = new List<CVar>();
            m_lookupById = new Dictionary<int, CVar>();
        }

        public int Count => m_variables.Count;

        #region IEnumerable implementation

        public IEnumerator<CVar> GetEnumerator()
        {
            return m_variables.GetEnumerator();
        }

        #endregion

        #region IEnumerable implementation

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_variables.GetEnumerator();
        }

        #endregion

        public void Add(CVar variable)
        {
            m_variables.Add(variable);
            m_lookupById.Add(variable.Id, variable);
        }

        public bool Remove(int id)
        {
            CVar variable;
            if (m_lookupById.TryGetValue(id, out variable))
            {
                m_lookupById.Remove(id);
                m_variables.Remove(variable);

                return true;
            }

            return false;
        }

        public CVar Find(int id)
        {
            CVar variable;
            return m_lookupById.TryGetValue(id, out variable) ? variable : null;
        }

        public CVar Find(string name)
        {
            foreach (CVar cvar in m_variables)
            {
                if (cvar.Name == name)
                {
                    return cvar;
                }
            }

            return null;
        }

        public void Clear()
        {
            m_variables.Clear();
            m_lookupById.Clear();
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class CVarRangeAttribute : Attribute
    {
        public readonly float max;
        public readonly float min;

        public CVarRangeAttribute(float min, float max)
        {
            this.min = min;
            this.max = max;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class CVarContainerAttribute : Attribute
    {
    }

    internal class CVarChangedDelegateList : BaseList<CVarChangedDelegate>
    {
        public CVarChangedDelegateList(int capacity)
            : base(NullCVarChangedDelegate, capacity)
        {
        }

        public void NotifyValueChanged(CVar cvar)
        {
            try
            {
                Lock();

                int elementsCount = list.Count;
                for (var i = 0; i < elementsCount; ++i) // do not update added items on that tick
                {
                    try
                    {
                        list[i](cvar);
                    }
                    catch (Exception e)
                    {
                        Log.e(e, "Exception while calling value changed delegate for '{0}'", cvar.Name);
                    }
                }
            }
            finally
            {
                Unlock();
            }
        }

        private static void NullCVarChangedDelegate(CVar cvar)
        {
        }
    }
}