using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Game.Entities;
using Game.Saves.Interfaces;
using Unity.VisualScripting;

namespace Game.Saves
{
    public static class SaveSystem
    {
        public static string SaveFilePath => GamePath;
#if UNITY_EDITOR
        private static readonly string GamePath = $"{Application.persistentDataPath}/rushrift_game_editor.save";
        private static readonly string SettingsPath = $"{Application.persistentDataPath}/rushrift_settings_editor.save";
#else
        private static readonly string GamePath = $"{Application.persistentDataPath}/rushrift_game_build.save";
        private static readonly string SettingsPath = $"{Application.persistentDataPath}/rushrift_settings_build.save";
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
            TData data;
            
            try
            {
                using var fs = new FileStream(path, FileMode.Open);
                data = (TData)formatter.Deserialize(fs);
            }
            catch (SerializationException e)
            {
                Debug.LogWarning($"[SaveSystem] Failed to deserialize {typeof(TData).Name}: {e.Message}. Resetting save.");

                var reset = createDefault();
                Save(reset, path);
                return reset;
            }
            
            // Parse versions
            var currentVersion = new Version(Application.version);
            Version saveVersion;

            if (!Version.TryParse(data.Version, out saveVersion))
            {
                Debug.LogWarning($"[SaveSystem] Invalid save version '{data.Version}', defaulting to 0.0.0");
                saveVersion = new Version(0, 0, 0);
            }
            
            // If the save is from the future (higher version)
            if (saveVersion.CompareTo(currentVersion) > 0)
            {
                Debug.LogWarning($"[SaveSystem] Save version ({saveVersion}) is newer than current game ({currentVersion}). Resetting.");

                var reset = createDefault();
                Save(reset, path);
                return reset;
            }
            
            // If outdated, apply migrations
            if (saveVersion.CompareTo(currentVersion) < 0 && migrations != null && migrations.Count > 0)
            {
#if true
                data = ApplyMigrations(data, saveVersion, currentVersion, migrations);
#else
                data = createDefault();
#endif
                Save(data, path);
            }

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

        private static TData ApplyMigrations<TData>(
            TData oldData,
            Version fromVersion,
            Version targetVersion,
            List<ISaveMigration<TData>> migrations
        ) where TData : BaseSaveData
        {
#if UNITY_EDITOR
            Debug.Log($"[SaveSystem] Applying migrations from {fromVersion} to {targetVersion}");
#endif
            migrations.Sort((a, b) => a.FromVersion.CompareTo(b.FromVersion));
            
            foreach (var migration in migrations)
            {
                if (migration.FromVersion.CompareTo(fromVersion) >= 0 &&
                    migration.ToVersion.CompareTo(targetVersion) <= 0)
                {
#if UNITY_EDITOR
                    Debug.Log($"[SaveSystem] Migrating {typeof(TData).Name}: {migration.FromVersion} → {migration.ToVersion}");
#endif
                    oldData = migration.Apply(oldData);
                    oldData.Version = migration.ToVersion.ToString();
                }
            }

            return oldData;
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