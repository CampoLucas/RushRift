using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Game.Entities;

public static class SaveAndLoad
{
    private static readonly string Path = $"{Application.persistentDataPath}/rushrift_{Application.version}.save";
    private static BinaryFormatter _formatter;
    private static FileStream _create;
    private static FileStream _open;

    public static string SaveFilePath => Path;

    public static void Save(this SaveData data)
    {
        _formatter = new BinaryFormatter();
        _create = new FileStream(Path, FileMode.Create);
        _formatter.Serialize(_create, data);
        _create.Close();
#if UNITY_EDITOR
        Debug.Log($"Saved data at: {Path}");
#endif
    }

    public static SaveData Load()
    {
        SaveData data = new();
        if (File.Exists(Path))
        {
            _formatter = new BinaryFormatter();
            _open = new FileStream(Path, FileMode.Open);
            data = _formatter.Deserialize(_open) as SaveData;
            _open.Close();
            return data;
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogWarning($"Save file not found in {Path}, creating new save");
#endif
            Save(data);
            return data;
        }
    }

    public static void Reset()
    {
        Save(new SaveData());
    }


    public static void AddMoney(int amount)
    {
        var data = Load();

        data.playerCurrency += amount;
        Save(data);
    }

    public static bool HasSaveData()
    {
        return File.Exists(Path);
    }
}