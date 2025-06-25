using Game.Entities;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;


[System.Serializable]
public class SaveData
{
    /// <summary>
    /// A property that in the case someone plays with an old save that didn't had the BestTime dictionary, it creates it.
    /// </summary>
    public Dictionary<int, float> BestTimes
    {
        get => _bestTimes ??= new Dictionary<int, float>();
        private set => _bestTimes = value;
    }

    /// <summary>
    /// A property that in the case someone plays with an old save that didn't had the UnlockedEffects dictionary, it creates it.
    /// </summary>
    public Dictionary<int, bool> UnlockedEffects
    {
        get => _unlockedEffects ??= new Dictionary<int, bool>();
        private set => _unlockedEffects = value;
    }

    /// <summary>
    /// A property that in the case someone plays with an old save that didn't had the Camera class, it creates it.
    /// </summary>
    public CameraSettings Camera
    {
        get => _camera ??= new CameraSettings();
        private set => _camera = value;
    }
    
    /// <summary>
    /// A property that in the case someone plays with an old save that didn't had the Sound class, it creates it.
    /// </summary>
    public SoundSettings Sound
    {
        get => _sound ??= new SoundSettings();
        private set => _sound = value;
    }
    
    public int playerCurrency;
    
    private Dictionary<int, bool> _unlockedEffects = new();
    private Dictionary<int, float> _bestTimes = new();
    private CameraSettings _camera = new();
    private SoundSettings _sound = new();
    
    public List<int> GetActiveEffects()
    {
        if (UnlockedEffects == null) return null;
        List<int> effects = new List<int>();
        foreach (var item in UnlockedEffects)
        {
            if (item.Value == true) effects.Add(item.Key);
        }

        return effects;
    }
}


[System.Serializable]
public class CameraSettings
{
    public float sensibility = .35f;
    public float smoothness = 30;
    public bool invertX = false;
    public bool invertY = false;
}

[System.Serializable]
public class SoundSettings
{
    public float masterVolume = 1;
    public float musicVolume = 1;
    public float sfxVolume = 1;
}


