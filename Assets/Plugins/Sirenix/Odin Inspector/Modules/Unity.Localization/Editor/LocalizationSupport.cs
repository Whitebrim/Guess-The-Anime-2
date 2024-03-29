//-----------------------------------------------------------------------
// <copyright file="LocalizationSupport.cs" company="Sirenix IVS">
// Copyright (c) Sirenix IVS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEngine.Localization;

#if UNITY_EDITOR

namespace Sirenix.OdinInspector.Modules.Localization.Editor
{
    public class LocalizedReferenceProcessor : OdinAttributeProcessor<LocalizedReference>
    {
        public override bool CanProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member)
        {
            return false;
        }

        public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
        {
            attributes.Add(new DrawWithUnityAttribute());
        }
    }

    public class LocalizedReferenceResolver : OdinPropertyResolver<LocalizedReference>
    {
        public override int ChildNameToIndex(string name)
        {
            throw new NotSupportedException();
        }

        public override int ChildNameToIndex(ref StringSlice name)
        {
            throw new NotSupportedException();
        }

        public override InspectorPropertyInfo GetChildInfo(int childIndex)
        {
            throw new NotSupportedException();
        }

        protected override int GetChildCount(LocalizedReference value)
        {
            return 0;
        }
    }
}
#endif