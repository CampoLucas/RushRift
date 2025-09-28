using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class DisplayContainer : MonoBehaviour
{
    [System.Serializable]
    public struct ContainerSetup
    {
        public DisplayElement element;
        public float startDelay;
    }

    [Header("Settings")]
    [SerializeField] private Vector2 startPos;
    [SerializeField] private ContainerSetup[] setups;
    
    [Header("References")]
    [SerializeField] private Canvas canvas;

    private void Awake()
    {
        if (canvas) canvas.enabled = false;
    }

    public void Play()
    {
        if (canvas) canvas.enabled = true;
        
        for (var i = 0; i < setups.Length; i++)
        {
            setups[i].element.DoAnim(startPos, setups[i].startDelay, null);
        }
    }
}
