using Game.Entities;
using System.Collections.Generic;


[System.Serializable]
public class SaveData
{
    public int playerCurrency;
    public Dictionary<int, bool> unlockedEffects = new();
    public CameraSettings CameraSettings = new();

    public List<int> GetActiveEffects()
    {
        if (unlockedEffects == null) return null;
        List<int> effects = new List<int>();
        foreach (var item in unlockedEffects)
        {
            if (item.Value == true) effects.Add(item.Key);
        }

        return effects;
    }
}

public struct CameraSettings
{
    public float Sensibility;
    public float Smoothness;

    public CameraSettings(float sensibility = .35f, float smoothness = 35f)
    {
        Sensibility = sensibility;
        Smoothness = smoothness;
    }
}
