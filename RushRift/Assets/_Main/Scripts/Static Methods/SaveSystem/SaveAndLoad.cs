using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Game.Entities;

public static class SaveAndLoad
{
    public static string SaveFilePath => GamePath;
    private static readonly string GamePath = $"{Application.persistentDataPath}/rushrift_{Application.version}.save";
    private static readonly string SettingsPath = $"{Application.persistentDataPath}/rushrift_settings.save";
    
    private static BinaryFormatter _formatter;
    private static FileStream _create;
    private static FileStream _open;

    #region Gameplay Save

    public static void SaveGame(this SaveData data)
    {
        _formatter = new BinaryFormatter();
        _create = new FileStream(GamePath, FileMode.Create);
        _formatter.Serialize(_create, data);
        _create.Close();
#if UNITY_EDITOR
        Debug.Log($"Saved data at: {GamePath}");
#endif
    }
    
    public static SaveData LoadGame()
    {
        SaveData data = new();
        if (File.Exists(GamePath))
        {
            _formatter = new BinaryFormatter();
            _open = new FileStream(GamePath, FileMode.Open);
            data = _formatter.Deserialize(_open) as SaveData;
            _open.Close();
            return data;
        }
        
#if UNITY_EDITOR
        Debug.LogWarning($"Save file not found in {GamePath}, creating new save");
#endif
        SaveGame(data);
        return data;
    }

    public static void ResetGame()
    {
        SaveGame(new SaveData());
    }
    
    public static bool HasSaveGame()
    {
        return File.Exists(GamePath);
    }

    #endregion

    #region Settings Save

    public static void SaveSettings(SettingsData data)
    {
        _formatter = new BinaryFormatter();
        using var fs = new FileStream(SettingsPath, FileMode.Create);
        _formatter.Serialize(fs, data);
#if UNITY_EDITOR
        Debug.Log($"Saved settings at: {SettingsPath}");
#endif
    }
    
    public static SettingsData LoadSettings()
    {
        if (!File.Exists(SettingsPath))
        {
#if UNITY_EDITOR
            Debug.LogWarning("No settings file found, creating new.");
#endif
            var newData = new SettingsData();
            SaveSettings(newData);
            return newData;
        }

        _formatter = new BinaryFormatter();
        using var fs = new FileStream(SettingsPath, FileMode.Open);
        return (SettingsData) _formatter.Deserialize(fs);
    }
    public static void ResetSettings() => SaveSettings(new SettingsData());
    

    #endregion
}