using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Game.Entities;
using Game.Saves.Interfaces;

namespace Game.Saves
{
    public static class SaveSystem
    {
        public static string SaveFilePath => GamePath;
#if UNITY_EDITOR
        private static readonly string GamePath = $"{Application.persistentDataPath}/rushrift_editor.save";
        private static readonly string SettingsPath = $"{Application.persistentDataPath}/rushrift_settings_editor.save";
#else
        private static readonly string GamePath = $"{Application.persistentDataPath}/rushrift_game.save";
        private static readonly string SettingsPath = $"{Application.persistentDataPath}/rushrift_settings.save";
#endif

        #region Generic

        private static void Save<TData>(TData data, string path) where TData : BaseSaveData
        {
            var formatter = new BinaryFormatter();
            using var fs = new FileStream(path, FileMode.Create);
            Save(data, path, formatter, fs);
        }

        private static void Save<TData>(TData data, string path, BinaryFormatter formatter, FileStream fs)
            where TData : BaseSaveData
        {
            data.Version = Application.version; // keep version updated.
            formatter.Serialize(fs, data);

#if UNITY_EDITOR
            Debug.Log($"[SaveSystem] Saved data at: {path}");
#endif
        }

        private static TData Load<TData>(string path, Func<TData> createDefault, List<ISaveMigration<TData>> migrations) where TData : BaseSaveData
        {
            if (!File.Exists(path))
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[SaveSystem] Save file not found in {path}, creating new save");
#endif
                var defaultData = createDefault();
                Save(defaultData, path);
                return defaultData;
            }


            var formatter = new BinaryFormatter();
            using var fs = new FileStream(path, FileMode.Open);
            var data = (TData)formatter.Deserialize(fs);

            if (data.Version == Application.version) return data;
// #if UNITY_EDITOR
//             Debug.LogWarning(
//                 $"[SaveSystem] Save version mismatch! Expected {Application.version}, found {data.Version}. Resetting save.");
// #endif

#if true
            data = ApplyMigrations(data, migrations);
#else
            data = createDefault();
#endif
            
            Save(data, path, formatter, fs);

            return data;
        }

        public static void Reset<TData>(string path, Func<TData> createDefault) where TData : BaseSaveData
        {
            Save(createDefault(), path);
        }

        public static bool HasSave<TData>(string path, Func<TData> createDefault) where TData : BaseSaveData
        {
            if (!File.Exists(path))
            {
                return false;
            }

            var formatter = new BinaryFormatter();
            using var fs = new FileStream(path, FileMode.Open);
            var data = (TData)formatter.Deserialize(fs);

            return data.Version == Application.version;
        }

        private static TData ApplyMigrations<TData>(TData data, List<ISaveMigration<TData>> migrations) where TData : BaseSaveData
        {
            var currentVersion = new Version(data.Version);
            
            foreach (var migration in migrations)
            {
                // Only apply migration from an older version
                if (migration.FromVersion.CompareTo(currentVersion) > 0)
                {
                    continue;
                }

                if (migration.FromVersion.CompareTo(currentVersion) <= 0 && migration.ToVersion.CompareTo(currentVersion) > 0)
                {
                    Debug.Log($"[SaveSystem] Desync between saves. Migrating save from {migration.FromVersion}, to {migration.ToVersion}");
                    
                    data = migration.Apply(data);
                    currentVersion = new Version(data.Version);
                }
            }

            return data;
        }

        #endregion



        #region Gameplay Save

        public static void SaveGame(this SaveData data)
        {
            Save(data, GamePath);
        }

        public static SaveData LoadGame()
        {
            return Load(GamePath, CreateNewGameSave, SaveMigrations.GameMigrations);
        }

        private static SaveData CreateNewGameSave()
        {
            return new SaveData();
        }

        public static void ResetGame()
        {
            Reset(GamePath, CreateNewGameSave);
        }

        public static bool HasSaveGame()
        {
            return HasSave(GamePath, CreateNewGameSave);
        }

        #endregion

        #region Settings Save

        public static void SaveSettings(SettingsData data)
        {
            Save(data, SettingsPath);
        }

        public static SettingsData LoadSettings()
        {
            return Load(SettingsPath, CreateNewSettingsSave, SaveMigrations.SettingsMigrations);
        }

        private static SettingsData CreateNewSettingsSave()
        {
            return new SettingsData();
        }

        public static void ResetSettings()
        {
            Reset(SettingsPath, CreateNewSettingsSave);
        }

        public static bool HasSettingsSave()
        {
            return HasSave(SettingsPath, CreateNewSettingsSave);
        }

        #endregion
    }
}