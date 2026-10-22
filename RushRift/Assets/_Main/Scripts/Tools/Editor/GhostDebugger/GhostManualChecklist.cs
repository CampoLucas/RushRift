using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.GhostDebugger
{
    public enum ManualCheckState
    {
        NotChecked,
        Pass,
        Fail
    }

    /// <summary>
    /// One hand-judged item with its result and notes.
    /// </summary>
    [Serializable]
    public sealed class ManualCheckItem
    {
        public string name;
        public ManualCheckState state;
        public string notes = "";
    }

    /// <summary>
    /// Checklist for what only a person can judge, saved to a file with the ghost name and date.
    /// </summary>
    [Serializable]
    public sealed class GhostManualChecklist
    {
        [Serializable]
        private sealed class SavedChecklist
        {
            public string ghost;
            public string date;
            public List<ManualCheckItem> items;
        }

        private static readonly string[] StateLabels = { "Not checked", "Pass", "Fail" };

        [SerializeField] private List<ManualCheckItem> items = new List<ManualCheckItem>();
        [SerializeField] private string lastSaved = "";

        public void Draw(GhostDebuggerContext context)
        {
            GhostDebuggerHelp.Info(context, "Watch the ghost play (Playback tab, slow motion helps) and mark each item. Save writes the results with the ghost name and date to the export folder.");

            if (items.Count == 0) Reset();

            foreach (var item in items)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(item.name, EditorStyles.boldLabel);
                        item.state = (ManualCheckState)GUILayout.Toolbar((int)item.state, StateLabels, GUILayout.Width(240));
                    }

                    item.notes = EditorGUILayout.TextField("Notes", item.notes);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save results")) Save(context);
                if (GUILayout.Button("Clear", GUILayout.Width(60)) && EditorUtility.DisplayDialog("Clear checklist", "Clear every result and note?", "Clear", "Cancel")) Reset();
            }

            if (!string.IsNullOrEmpty(lastSaved)) EditorGUILayout.LabelField("Saved", lastSaved, EditorStyles.miniLabel);
        }

        private void Reset()
        {
            items = new List<ManualCheckItem>
            {
                new ManualCheckItem { name = "No jitter on corners" },
                new ManualCheckItem { name = "No rotation snapping" },
                new ManualCheckItem { name = "No clipping through geometry" },
                new ManualCheckItem { name = "Ghost visible and translucent" },
                new ManualCheckItem { name = "Ghost does not block the player" }
            };
            lastSaved = "";
        }

        private void Save(GhostDebuggerContext context)
        {
            var ghost = string.Join("_", context.RunName.Split(Path.GetInvalidFileNameChars())).Replace(GhostDebuggerService.Extension, "");
            var now = DateTime.Now;
            var saved = new SavedChecklist { ghost = context.RunName, date = now.ToString("yyyy-MM-dd HH:mm:ss"), items = items };
            var path = Path.Combine(context.ExportFolder, $"{ghost}_{now:yyyyMMdd_HHmmss}_checklist.json");

            Directory.CreateDirectory(context.ExportFolder);
            File.WriteAllText(path, JsonUtility.ToJson(saved, true));
            lastSaved = path;
        }
    }
}
