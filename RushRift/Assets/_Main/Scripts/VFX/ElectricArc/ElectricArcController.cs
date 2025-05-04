using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace Game.VFX
{
    public class ElectricArcController : MonoBehaviour
    {
        public static int Pos1 = Shader.PropertyToID("Pos1");
        public static int Pos2 = Shader.PropertyToID("Pos2");
        public static int Pos3 = Shader.PropertyToID("Pos3");
        public static int Pos4 = Shader.PropertyToID("Pos4");

        [Header("Settings")]
        [SerializeField] private float arcOffset = .5f;
        [SerializeField] private bool startEnabled;
        
        [Header("Visual Effects")]
        [SerializeField] private VisualEffect[] electricArcs;
        

        private Transform _start;
        private Vector3 _end;

        private float _duration;
        
        private void Awake()
        {
            Enable(startEnabled);
        }

        private void LateUpdate()
        {
            var start = _start.position;
            var dir = _end - start;
            var mid = (start + _end) * 0.5f;

            var perp = Vector3.Cross(dir.normalized, Vector3.forward).normalized;

            var mid1 = Vector3.Lerp(start, mid, 0.5f) + perp * arcOffset;
            var mid2 = Vector3.Lerp(mid, _end, 0.5f) - perp * arcOffset;
            
            SetPosition(Pos1, start);
            SetPosition(Pos2, mid1);
            SetPosition(Pos3, mid2);
            SetPosition(Pos4, _end);
            
            if (_duration <= 0)
            {
                Destroy(gameObject);
            }

            _duration -= Time.deltaTime;
        }

        public void SetPosition(Transform start, Vector3 end)
        {
            _start = start;
            _end = end;
        }

        public void Enable(bool value)
        {
            for (var i = 0; i < electricArcs.Length; i++)
            {
                electricArcs[i].enabled = value;
            }
        }

        public void SetDuration(float duration)
        {
            _duration = duration;
        }

        private void SetPosition(int posID, Vector3 pos)
        {
            for (var i = 0; i < electricArcs.Length; i++)
            {
                electricArcs[i].SetVector3(posID, pos);
            }
        }

    }
}
