using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class SaveAndLoad
{
    private static string path = Application.persistentDataPath + "/save3.data";
    private static BinaryFormatter formatter;
    private static FileStream create;
    private static FileStream open;
    /// <summary>
    /// Save Colectables Unlocked to a file.
    /// </summary>
    /// <param name="arrays"></param>
    /// 
    //public static void Save(ColectablesArrays arrays)
    //{
    //    formatter = new BinaryFormatter();
    //    create = new FileStream(path, FileMode.Create);
    //    ColectableData data = new ColectableData(arrays);
    //    formatter.Serialize(create, data);
    //    create.Close();
    //    Debug.Log("Game Saved");
    //}

    public static void Save(int score)
    {
        formatter = new BinaryFormatter();
        create = new FileStream(path, FileMode.Create);
        SaveData data = new SaveData(score);
        formatter.Serialize(create, data);
        create.Close();
        Debug.Log("Game Saved");
    }

    /// <summary>
    /// Loads Colectables Unlocked.
    /// </summary>
    /// <returns></returns>
    //public static ColectableData LoadColectables()
    //{
    //    ColectableData data;
    //    if (File.Exists(path))
    //    {
    //        formatter = new BinaryFormatter();
    //        open = new FileStream(path, FileMode.Open);
    //        data = formatter.Deserialize(open) as ColectableData;
    //        open.Close();
    //        Debug.Log("Loaded Game");
    //        return data;
    //    }
    //    else
    //    {
    //        Debug.LogError("Save file not found in" + path);   
    //        return null;
    //    }
    //}

    public static SaveData Load()
    {
        SaveData data;
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
            Debug.LogError("Save file not found in" + path);
            return null;
        }
    }
} 
    

