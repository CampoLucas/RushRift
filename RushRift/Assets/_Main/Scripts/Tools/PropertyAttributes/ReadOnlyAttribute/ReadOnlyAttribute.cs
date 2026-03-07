using UnityEngine;

namespace Tools.Scripts.PropertyAttributes
{
    public class ReadOnlyAttribute : PropertyAttribute
    {
        
    }
    
    public class ReadOnlyIfAttribute : PropertyAttribute
    {
        public readonly string BoolFieldName;
        public readonly bool Value;

        public ReadOnlyIfAttribute(string boolFieldName, bool value = false)
        {
            BoolFieldName = boolFieldName;
            Value = value;
        }
    }
    
    public class HideIfAttribute : PropertyAttribute
    {
        public readonly string BoolFieldName;
        public readonly bool Value;

        public HideIfAttribute(string boolFieldName, bool value = false)
        {
            BoolFieldName = boolFieldName;
            Value = value;
        }
    }
}