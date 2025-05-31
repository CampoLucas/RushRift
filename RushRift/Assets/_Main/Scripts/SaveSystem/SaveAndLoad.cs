using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Game.Entities;

public class SaveAndLoad
{
    private static string path = Application.persistentDataPath + "/save3.data";
    private static BinaryFormatter formatter;
    private static FileStream create;
    private static FileStream open;

    public static void Save(SaveData data)
    {
        formatter = new BinaryFormatter();
        create = new FileStream(path, FileMode.Create);
        formatter.Serialize(create, data);
        create.Close();
        Debug.Log("Game Saved");
    }


    public static SaveData Load()
    {
        SaveData data = new();
        if (File.Exists(path))
        {
            formatter = new BinaryFormatter();
            open = new FileStream(path, FileMode.Open);
            data = formatter.Deserialize(open) as SaveData;
            open.Close();
            Debug.Log("Loaded Game");
            return data;
        }
        else
        {
            Debug.LogWarning($"Save file not found in {path}, creating new save" );
            Save(data);
            return data;
        }
    }
} 
    

