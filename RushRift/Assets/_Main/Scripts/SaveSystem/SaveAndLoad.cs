using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Game.Entities;

public class SaveAndLoad
{
    //private static string path = Application.persistentDataPath + "/save64mario.data";
    private static readonly string Path = $"{Application.persistentDataPath}/rushrift_{Application.version}.save";
    private static BinaryFormatter _formatter;
    private static FileStream _create;
    private static FileStream _open;

    public static void Save(SaveData data)
    {
        _formatter = new BinaryFormatter();
        _create = new FileStream(Path, FileMode.Create);
        _formatter.Serialize(_create, data);
        _create.Close();
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
            Debug.LogWarning($"Save file not found in {Path}, creating new save" );
            Save(data);
            return data;
        }
    }
} 
    

