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
        [FormerlySerializedAs("container")] public DisplayElement element;
        public float startDelay;
    }

    [SerializeField] private float startPos = -2000;
    [SerializeField] private ContainerSetup[] setups;

    public void Play()
    {
        for (var i = 0; i < setups.Length; i++)
        {
            setups[i].element.DoAnim(startPos, setups[i].startDelay, null);
        }
    }
}
