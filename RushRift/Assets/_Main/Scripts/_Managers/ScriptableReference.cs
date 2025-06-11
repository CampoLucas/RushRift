using Game.Entities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct EffectsReferences
{
    public int ID;
    public Effect effect;
}

public  class ScriptableReference : MonoBehaviour
{
    private static ScriptableReference _instance;
    public List<EffectsReferences> effectsReferences = new();
    public static ScriptableReference Instance => _instance;

    private void Awake()
    {
        if (_instance == null) _instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(this.gameObject);
        
    }
}
