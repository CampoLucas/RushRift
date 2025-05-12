using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace Game.Entities.Components
{
    [System.Serializable]
    public class SpeedLinesData
    {
        [Header("Settings")]
        [SerializeField] private float minVelocity, maxVelocity;

        [Header("Effect Ranges")]
        [Header("Radius")]
        [SerializeField] private float minRadiusRange;
        [SerializeField] private float maxRadiusRange;
        
        [Header("XScale")]
        [SerializeField] private Vector2 minXScaleRange;
        [SerializeField] private Vector2 maxXScaleRange;
        
        [Header("YScale")]
        [SerializeField] private Vector2 minYScaleRange;
        [SerializeField] private Vector2 maxYScaleRange;
        
        [Header("Velocity")]
        [SerializeField] private Vector2 minVelocityRange;
        [SerializeField] private Vector2 maxVelocityRange;
        
        [Header("Rate")]
        [SerializeField] private float minRateRange;
        [SerializeField] private float maxRateRange;

        [Header("FOV")]
        [SerializeField] private float minFOV;
        [SerializeField] private float maxFOV;

        public float SetEffect(float currentVelocity, VisualEffect effect)
        {
            var f = Mathf.Clamp01((currentVelocity - minVelocity) / (maxVelocity - minVelocity));

            if (f > 0)
            {
                effect.SetFloat("Radius", Mathf.Lerp(minRadiusRange, maxRadiusRange, f));
                effect.SetVector2("XScaleRange", Vector2.Lerp(minXScaleRange, maxXScaleRange, f));
                effect.SetVector2("YScaleRange", Vector2.Lerp(minYScaleRange, maxYScaleRange, f));
                effect.SetVector2("VelocityRange", Vector2.Lerp(minVelocityRange, maxVelocityRange, f));
                effect.SetInt("SpawnRate", (int)Mathf.Lerp(minRateRange, maxRateRange, f));
            }
            
            return f;
        }
    }
}
