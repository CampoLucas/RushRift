using System;
using System.Collections.Generic;
using System.Linq;
using Game.Levels;
using Tools.PlayHook;
using Tools.PlayHook.Elements.Menu;
using UnityEditor;
using UnityEngine;
using MenuItem = Tools.PlayHook.Elements.Menu.MenuItem;

namespace Game.LeaderboardDebugger
{
    public static class LeaderboardLevelSource
    {
        private static List<GameModeSO> gameModes = new List<GameModeSO>();
        private static List<BaseLevelSO> levels = new List<BaseLevelSO>();

        public static void Reload()
        {
            gameModes = AssetDatabase.FindAssets($"t:{nameof(GameModeSO)}")
                .Select(guid => AssetDatabase.LoadAssetAtPath<GameModeSO>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(mode => mode != null)
                .OrderBy(mode => mode.name)
                .ToList();

            levels = AssetDatabase.FindAssets($"t:{nameof(BaseLevelSO)}")
                .Select(guid => AssetDatabase.LoadAssetAtPath<BaseLevelSO>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(level => level != null)
                .OrderBy(level => level.LevelID)
                .ToList();
        }

        public static bool TryResolve(bool useCurrent, int manualLevel, out int level, out string source)
        {
            level = manualLevel;
            source = "manual id";

            if (!useCurrent) return true;

            if (Application.isPlaying && GlobalLevelManager.CurrentLevel.TryGet(out var current))
            {
                level = current.LevelID;
                source = $"Play Mode level {current.LevelName}";
                return true;
            }

            var selected = GetPlayHookLevel();
            if (selected != null)
            {
                level = selected.LevelID;
                source = $"PlayHook selection {selected.LevelName}";
                return true;
            }

            source = "no current level found, using the manual id";
            return false;
        }

        public static BaseLevelSO GetPlayHookLevel()
        {
            var path = PlayLevelSelectionBridge.GetLevelPath();
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<BaseLevelSO>(path);
        }

        public static string GetLabel(int levelId)
        {
            var level = levels.FirstOrDefault(candidate => candidate.LevelID == levelId);
            return level == null ? $"Level {levelId}" : Describe(level);
        }

        public static void ShowDropdown(Rect buttonRect, int selectedLevel, Action<int> onSelected)
        {
            Reload();

            var entries = new List<MenuEntry>();

            if (gameModes.Count == 0)
            {
                entries.Add(new MenuItem("No GameModes found", null, false, () => true));
            }

            foreach (var mode in gameModes)
            {
                var group = new MenuGroup(mode.DisplayName);
                var modeLevels = mode.Levels == null ? new List<BaseLevelSO>() : mode.Levels.Where(level => level != null).ToList();

                if (modeLevels.Count == 0) group.Add(new MenuItem("Empty", null, false, () => true));

                foreach (var level in modeLevels)
                {
                    var id = level.LevelID;
                    group.Add(new MenuItem(Describe(level), () => onSelected(id), id == selectedLevel, () => false));
                }

                entries.Add(group);
            }

            entries.Add(new MenuSeparator());

            var all = new MenuGroup("All Levels");
            if (levels.Count == 0) all.Add(new MenuItem("No levels found", null, false, () => true));

            foreach (var level in levels)
            {
                var id = level.LevelID;
                all.Add(new MenuItem(Describe(level), () => onSelected(id), id == selectedLevel, () => false));
            }

            entries.Add(all);

            var menu = new GenericMenu();
            foreach (var entry in entries) entry.Setup(ref menu);
            menu.DropDown(buttonRect);
        }

        private static string Describe(BaseLevelSO level)
        {
            return $"{level.LevelID:D2}: {level.LevelName} ({level.GetType().Name})";
        }
    }
}
