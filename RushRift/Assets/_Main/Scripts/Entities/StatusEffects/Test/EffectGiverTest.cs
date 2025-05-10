using System;
using System.Collections;
using System.Collections.Generic;
using Game.Entities;
using UnityEngine;

public class EffectGiverTest : MonoBehaviour
{
    public Effect effect;

    private void OnTriggerEnter(Collider other)
    {
        if (!effect) return;
        if (other.gameObject.TryGetComponent<IController>(out var controller))
        {
            Debug.Log("Give effect");
            effect.ApplyEffect(controller);
            
            Destroy(gameObject);
        }
    }
}
