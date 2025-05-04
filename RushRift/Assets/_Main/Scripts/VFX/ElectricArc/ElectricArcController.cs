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
        [SerializeField] private float speed = 50f;
        [SerializeField] private float time = .1f;
        
        [Header("Visual Effects")]
        [SerializeField] private VisualEffect[] electricArcs;
        

        private Transform _snapPos;
        private Vector3 _start;
        private Vector3 _end;

        private float _duration;
        private float _timer;
        
        private void Awake()
        {
            Enable(startEnabled);
        }

        private void LateUpdate()
        {
            var start = Vector3.zero;
            
            if (_duration > 0)
            {
                _start = _snapPos.position;
                start = _start;
                _duration -= Time.deltaTime;
            }
            else
            {
                // when it isn't snapped, the start gets closer to the end, when the distance from the two is less than 0.01 the game object is destroyed
                _timer += Time.deltaTime;

                start = Vector3.Lerp(_start, _end, _timer / time);
                // _start = Vector3.MoveTowards(_start, _end, Time.deltaTime * speed); // Speed
                //
                // if (Vector3.Distance(_start, _end) < 0.01f)
                // {
                //     Destroy(gameObject);
                // }

                
                
                if (_timer >= time)
                {
                    Destroy(gameObject);
                }
            }
            
            var dir = (_end - start).normalized;
            var mid = (start + _end) * 0.5f;

            var worldUp = Vector3.up;
            
            if (Vector3.Dot(dir, worldUp) > 0.99f)
            {
                worldUp = Vector3.forward;
            }
            
            var perp = Vector3.Cross(dir, worldUp).normalized;

            var mid1 = Vector3.Lerp(start, mid, 0.5f) + perp * arcOffset;
            var mid2 = Vector3.Lerp(mid, _end, 0.5f) - perp * arcOffset;
            
            SetPosition(Pos1, start);
            SetPosition(Pos2, mid1);
            SetPosition(Pos3, mid2);
            SetPosition(Pos4, _end);
            
            // if (_duration <= 0)
            // {
            //     Destroy(gameObject);
            // }
            //
            // _duration -= Time.deltaTime;
        }

        public void SetPosition(Transform start, Vector3 end, float snapDuration)
        {
            _duration = snapDuration;
            _snapPos = start;
            _end = end;
        }

        public void Enable(bool value)
        {
            for (var i = 0; i < electricArcs.Length; i++)
            {
                electricArcs[i].enabled = value;
            }
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
