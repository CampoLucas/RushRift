#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Main.Scripts.Tools
{
    internal static class HierarchySort
    {
        [MenuItem("Tools/Hierarchy/Sort Selected Naturally (A→Z)")]
        private static void SortSelectedAsc() => SortSelected(true);

        [MenuItem("Tools/Hierarchy/Sort Selected Naturally (Z→A)")]
        private static void SortSelectedDesc() => SortSelected(false);

        [MenuItem("Tools/Hierarchy/Sort Children Naturally (A→Z)")]
        private static void SortChildrenAsc() => SortChildren(true);

        [MenuItem("Tools/Hierarchy/Sort Children Naturally (Z→A)")]
        private static void SortChildrenDesc() => SortChildren(false);

        [MenuItem("Tools/Hierarchy/Sort Selected Naturally (A→Z)", true)]
        [MenuItem("Tools/Hierarchy/Sort Selected Naturally (Z→A)", true)]
        [MenuItem("Tools/Hierarchy/Sort Children Naturally (A→Z)", true)]
        [MenuItem("Tools/Hierarchy/Sort Children Naturally (Z→A)", true)]
        private static bool ValidateHasSelection() => Selection.transforms != null && Selection.transforms.Length > 0;

        private static readonly NaturalUnityNameComparer NameComparer = new NaturalUnityNameComparer();

        private static void SortSelected(bool ascending)
        {
            var selected = Selection.transforms;
            var groups = selected.GroupBy(t => new ParentKey(t.parent, t.gameObject.scene));
            foreach (var g in groups)
            {
                int start = g.Min(t => t.GetSiblingIndex());
                var ordered = ascending
                    ? g.OrderBy(t => t.name, NameComparer).ToArray()
                    : g.OrderBy(t => t.name, NameComparer).Reverse().ToArray();

                for (int i = 0; i < ordered.Length; i++)
                {
                    var tr = ordered[i];
                    if (tr.GetSiblingIndex() == start + i) continue;
                    Undo.RecordObject(tr, "Sort Selected Naturally");
                    tr.SetSiblingIndex(start + i);
                    EditorSceneManager.MarkSceneDirty(tr.gameObject.scene);
                }
            }
        }

        private static void SortChildren(bool ascending)
        {
            foreach (var parent in Selection.transforms)
            {
                var children = Enumerable.Range(0, parent.childCount).Select(i => parent.GetChild(i)).ToArray();
                var ordered = ascending
                    ? children.OrderBy(t => t.name, NameComparer).ToArray()
                    : children.OrderBy(t => t.name, NameComparer).Reverse().ToArray();

                for (int i = 0; i < ordered.Length; i++)
                {
                    var tr = ordered[i];
                
                    if (tr.GetSiblingIndex() == i) continue;
                    Undo.RecordObject(tr, "Sort Children Naturally");
                    tr.SetSiblingIndex(i);
                    EditorSceneManager.MarkSceneDirty(tr.gameObject.scene);
                }
            }
        }

        private readonly struct ParentKey
        {
            public readonly Transform parent;
            public readonly Scene scene;
            public ParentKey(Transform p, Scene s) { parent = p; scene = s; }
            public override int GetHashCode() => (parent ? parent.GetHashCode() : 0) ^ scene.handle.GetHashCode();
            public override bool Equals(object obj) => obj is ParentKey k && k.parent == parent && k.scene == scene;
        }

        private sealed class NaturalUnityNameComparer : IComparer<string>
        {
            static readonly Regex ParenNumber = new Regex(@"^\s*(.*?)(?:\s*\((\d+)\))?\s*$",
                RegexOptions.Compiled | RegexOptions.CultureInvariant);

            public int Compare(string a, string b)
            {
                if (ReferenceEquals(a, b)) return 0;
                if (a == null) return -1;
                if (b == null) return 1;

                var ma = ParenNumber.Match(a);
                var mb = ParenNumber.Match(b);

                var baseA = ma.Success ? ma.Groups[1].Value : a;
                var baseB = mb.Success ? mb.Groups[1].Value : b;

                int baseCmp = string.Compare(baseA, baseB, StringComparison.OrdinalIgnoreCase);
                if (baseCmp != 0) return baseCmp;

                bool hasNumA = ma.Success && ma.Groups[2].Success;
                bool hasNumB = mb.Success && mb.Groups[2].Success;

                if (hasNumA && hasNumB)
                {
                    var numA = ParseInt(ma.Groups[2].Value);
                    var numB = ParseInt(mb.Groups[2].Value);
                    int numCmp = numA.CompareTo(numB);
                    if (numCmp != 0) return numCmp;
                }
            
                else if (hasNumA != hasNumB)
                {
                    return hasNumA ? 1 : -1;
                }
            
                return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
            }

            static int ParseInt(string s)
            {
                int.TryParse(s, out var v);
                return v;
            }
        }
    }
}
#endif
