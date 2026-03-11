using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public class Line
{
    public string Text => text;
    public AudioClip Clip => clip;

    [FormerlySerializedAs("line")]
    [TextArea(3, 10)]
    [SerializeField] private string text;
    [SerializeField] private AudioClip clip;
}
