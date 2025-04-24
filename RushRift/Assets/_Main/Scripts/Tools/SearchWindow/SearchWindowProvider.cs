using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Experimental.GraphView;
#endif
using UnityEngine;

namespace Game.Tools
{
    public class SearchWindowProvider : ScriptableObject
#if UNITY_EDITOR
        , ISearchWindowProvider
#endif
        , IDisposable
    {
        private string[] _listItems;
        private Type[] _typeList;
        private Action<Type> _onSelectedCallback;

        public SearchWindowProvider(string[] listItems, Type[] typeList, Action<Type> callback)
        {
            _listItems = listItems;
            _typeList = typeList;
            _onSelectedCallback = callback;
        }
        
#if UNITY_EDITOR
        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var list = new List<SearchTreeEntry>();

            if (_listItems.Length == 0)
            {
                list.Add(new SearchTreeGroupEntry(new GUIContent("List"), 0));
                return list;
            }

            var sortedItems = _listItems
                .Select((item, index) => (item, _typeList[index])) // Pair items with types
                .OrderBy(pair => pair.item, StringComparer.Ordinal) // Sort by string
                .ToList();

            _listItems = sortedItems.Select(p => p.item).ToArray();
            _typeList = sortedItems.Select(p => p.Item2).ToArray();

            var groups = new HashSet<string>();
            for (var i = 0; i < _listItems.Length; i++)
            {
                var entryTitle = _listItems[i].Split('/');
                var groupName = "";

                for (var j = 0; j < entryTitle.Length - 1; j++)
                {
                    groupName += entryTitle[j];

                    if (!groups.Contains(groupName))
                    {
                        list.Add(new SearchTreeGroupEntry(new GUIContent(entryTitle[j]), j + 1));
                        groups.Add(groupName);
                    }

                    groupName += "/";
                }

                var entry = new SearchTreeEntry(new GUIContent(entryTitle.Last()))
                {
                    level = entryTitle.Length,
                    userData = _typeList[i]  // Assign the correct Type
                };

                list.Add(entry);
            }

            return list;
        }
#endif

        private int Comparison(string a, string b)
        {
            var splits1 = a.Split('/');
            var splits2 = b.Split('/');
            for (var i = 0; i < splits1.Length; i++)
            {
                if (i >= splits2.Length)
                {
                    return 1;
                }

                var value = splits1[i].CompareTo(splits2[i]);

                if (value != 0)
                {
                    if (splits1.Length != splits2.Length && (i == splits1.Length - 1 || i == splits2.Length - 1)) return splits1.Length < splits2.Length ? 1 : -1;
                    return value;
                }
            }

            return 0;
        }

#if UNITY_EDITOR
        public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            if (searchTreeEntry.userData is Type type)
            {
                _onSelectedCallback?.Invoke(type);
            }
            Dispose();
            return true;
        }

        public static void OpenSearchTypeWindow<TObject>(Action<Type> callback)
        {
            var pos = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
            var values = GetValues<TObject>();

#if false
            var provider = new SearchWindowProvider(values.Item1, values.Item2, callback);
            
#else
            var provider = CreateInstance<SearchWindowProvider>();
            provider._listItems = values.Item1;
            provider._typeList = values.Item2;
            provider._onSelectedCallback = callback;
#endif
            
            
            var swc = new SearchWindowContext(pos);
            SearchWindow.Open(swc, provider);
        }
#endif

#if UNITY_EDITOR
        private static (string[], Type[]) GetValues<TType>()
        {
            var types = TypeCache.GetTypesDerivedFrom<TType>();
            
            var typesNameList = new List<string>();
            var typesList = new List<Type>();
            var typeList = new List<Type>();
            
            for (var i = 0; i < types.Count; i++)
            {
                typeList.Clear();
                var t = types[i];
                if (t.IsAbstract || t.IsSealed || t.ContainsGenericParameters) continue;
                
                GetFullLengthString(typeof(TType), t, ref typeList);
                
                typesNameList.Add(Convert(typeList));
                typesList.Add(t);
            }

            return (typesNameList.ToArray(), typesList.ToArray());
        }
#endif

        private static void GetFullLengthString(in Type parentType, in Type currentType, ref List<Type> typeArray)
        {
            typeArray.Add(currentType);
            if (currentType == parentType) return;
            
            GetFullLengthString(parentType, currentType.BaseType, ref typeArray);
        }

        private static string Convert(in List<Type> type)
        {
            var result = "";
            for (var i = type.Count - 1; i >= 0; i--)
            {
                if (i == 0)
                {
                    result += type[i].Name;
                }
                else
                {
                    result += type[i].Name + "/";
                }
            }

            return result;
        }

        public void Dispose()
        {
            _listItems = null;
            _onSelectedCallback = null;
        }
    }
}