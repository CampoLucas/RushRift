using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.VFX
{
    public class LaserVFXController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LineRenderer _laserRenderer;
        [SerializeField] private ParticleSystem _muzzle;
        [SerializeField] private List<Material> _materials;

        [Header("Visual")]
        [SerializeField] private Color activeColor;
        [SerializeField] private Color idleColor;
        [SerializeField] private float lengthMultiplier = .25f;
        
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        private static readonly int Length = Shader.PropertyToID("_Length");

        public void Init(Material[] materials)
        {
            _materials = new List<Material>(materials);
        }
        
        public void SetEndPos(Vector3 endPos)
        {
            _laserRenderer.SetPosition(1, transform.InverseTransformPoint(endPos));
            _laserRenderer.material.SetFloat(Length, Vector3.Distance(transform.position, endPos) * lengthMultiplier);
            //Debug.Log($"Set Pos to {endPos}");
        }

        private void LerpColor(Color color1, Color color2, float t)
        {
            for (var i = 0; i < _materials.Count; i++)
            {
                var m = _materials[i];
                if (m == null) continue;
                
                m.SetColor(EmissionColor, Color.Lerp(color1, color2, t));
            }
        }
    }
}