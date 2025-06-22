using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace MyTools.Global.Editor
{
    public static class EditorUtils
    {
        public static void AddManipulators(this VisualElement element, IManipulator[] manipulators)
        {
            for (var i = 0; i < manipulators.Length; i++)
            {
                var m = manipulators[i];
                if (m == null) return;
                element.AddManipulator(m);
            }
        }
        
        public static void RemoveManipulators(this VisualElement element, IManipulator[] manipulators)
        {
            for (var i = 0; i < manipulators.Length; i++)
            {
                var m = manipulators[i];
                if (m == null) return;
                element.RemoveManipulator(m);
            }
        }

        public static IManipulator CreateContentZoomer(float min, float max)
        {
            var zoomer = new ContentZoomer();
            zoomer.minScale = min;
            zoomer.maxScale = max;

            return zoomer;
        }
        
        private static object GetValue_Imp(object source, string name)
        {
            if (source == null)
                return null;
            var type = source.GetType();

            while (type != null)
            {
                var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (f != null)
                    return f.GetValue(source);

                var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p != null)
                    return p.GetValue(source, null);

                type = type.BaseType;
            }
            return null;
        }

        private static object GetValue_Imp(object source, string name, int index)
        {
            var enumerable = GetValue_Imp(source, name) as System.Collections.IEnumerable;
            if (enumerable == null) return null;
            var enm = enumerable.GetEnumerator();

            for (var i = 0; i <= index; i++)
            {
                if (!enm.MoveNext()) return null;
            }
            return enm.Current;
        }
        
        public static object GetTargetObjectOfProperty(SerializedProperty prop)
        {
            if (prop == null) return null;

            var path = prop.propertyPath.Replace(".Array.data[", "[");
            object obj = prop.serializedObject.targetObject;
            var elements = path.Split('.');
            foreach (var element in elements)
            {
                if (element.Contains("["))
                {
                    var elementName = element.Substring(0, element.IndexOf("["));
                    var index = System.Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                    obj = GetValue_Imp(obj, elementName, index);
                }
                else
                {
                    obj = GetValue_Imp(obj, element);
                }
            }
            return obj;
        }
    }
}