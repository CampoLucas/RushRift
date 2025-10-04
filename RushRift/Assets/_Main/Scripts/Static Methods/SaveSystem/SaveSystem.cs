using System;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Game.Entities;

public static class SaveSystem
{
    public static string SaveFilePath => GamePath;
    private static readonly string GamePath = $"{Application.persistentDataPath}/rushrift_game.save";
    private static readonly string SettingsPath = $"{Application.persistentDataPath}/rushrift_settings.save";
    
    #region Generic

    private static void Save<TData>(TData data, string path) where TData : BaseSaveData
    {
        var formatter = new BinaryFormatter();
        using var fs = new FileStream(path, FileMode.Create);
        Save(data, path, formatter, fs);
    }
    
    private static void Save<TData>(TData data, string path, BinaryFormatter formatter, FileStream fs) where TData : BaseSaveData
    {
        data.version = Application.version; // keep version updated.
        formatter.Serialize(fs, data);
        
#if UNITY_EDITOR
        Debug.Log($"[SaveSystem] Saved data at: {path}");
#endif
    }
    
    private static TData Load<TData>(string path, Func<TData> createDefault) where TData : BaseSaveData
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
        var data = (TData) formatter.Deserialize(fs);

        if (data.version == Application.version) return data;
#if UNITY_EDITOR
        Debug.LogWarning($"[SaveSystem] Save version mismatch! Expected {Application.version}, found {data.version}. Resetting save.");
#endif

        data = createDefault();
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

        return data.version == Application.version;
    }

    #endregion
    
    
    
    #region Gameplay Save

    public static void SaveGame(this SaveData data)
    {
        Save(data, GamePath);
    }
    
    public static SaveData LoadGame()
    {
        return Load(GamePath, CreateNewGameSave);
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
        return Load(SettingsPath, CreateNewSettingsSave);
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