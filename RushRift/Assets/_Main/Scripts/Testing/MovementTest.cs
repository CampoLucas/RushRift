#if UNITY_EDITOR
#define MOVEMENT_TEST_ENABLED
#endif

using System;
using Unity.Collections;
using UnityEngine;

#if MOVEMENT_TEST_ENABLED
using MyTools.Global;
using System.Collections.Generic;
using Game.Entities;
using Game.Entities.Components.MotionController;
using Game.Utils;
#endif

public class MovementTest : MonoBehaviour
{
#if MOVEMENT_TEST_ENABLED
    [Header("Stats Window")]
    [Tooltip("Number of fixed-physics samples to average over (e.g., 120 \u2248 2s at 60 FPS)")]
    public int windowSize = 120;
    
    [Header("Logs")]
    [ReadOnly] public List<string> logs = new();
    
    private PlayerController _target;
    private Rigidbody _rigidbody;
    private MotionController _motionController;
    
    // latest raw values (for instant readout)
    private float _velMag, _horMag, _yVel;
    
    // rolling window stats
    private RollingWindow _velStat;
    private RollingWindow _horStat;
    private RollingWindow _yStat;
    
    // simple helper: rolling window with O(1) mean/min/max updates
    private struct RollingWindow
    {
        private float[] _buf;
        private int _idx;
        private int _count;
        private double _sum;   // keep precision
        private float _min, _max;

        public RollingWindow(int capacity)
        {
            _buf = new float[Mathf.Max(1, capacity)];
            _idx = 0;
            _count = 0;
            _sum = 0;
            _min = float.PositiveInfinity;
            _max = float.NegativeInfinity;
        }

        public void Clear()
        {
            Array.Clear(_buf, 0, _buf.Length);
            _idx = 0;
            _count = 0;
            _sum = 0;
            _min = float.PositiveInfinity;
            _max = float.NegativeInfinity;
        }

        public void Add(float v)
        {
            if (_count < _buf.Length)
            {
                _buf[_idx] = v;
                _sum += v;
                _count++;
                if (v < _min) _min = v;
                if (v > _max) _max = v;
                _idx = (_idx + 1) % _buf.Length;
                return;
            }

            // full: remove oldest, add newest
            var old = _buf[_idx];
            _buf[_idx] = v;
            _idx = (_idx + 1) % _buf.Length;

            _sum += v - old;

            // min/max may be invalidated if we replaced the extremum → recompute lazily
            if (old == _min || old == _max || v < _min || v > _max)
            {
                RecomputeMinMax();
            }
        }

        private void RecomputeMinMax()
        {
            float mn = float.PositiveInfinity, mx = float.NegativeInfinity;
            int n = _count;
            for (int i = 0; i < n; i++)
            {
                var val = _buf[i];
                if (val < mn) mn = val;
                if (val > mx) mx = val;
            }
            _min = (n > 0) ? mn : 0f;
            _max = (n > 0) ? mx : 0f;
        }

        public int Count => _count;
        public float Mean => _count > 0 ? (float)(_sum / _count) : 0f;
        public float Min  => _count > 0 ? _min : 0f;
        public float Max  => _count > 0 ? _max : 0f;
    }
#endif

    private void Awake()
    {
#if !MOVEMENT_TEST_ENABLED
        Destroy(gameObject);
#endif
        
    }
    
    private void Start()
    {
#if MOVEMENT_TEST_ENABLED
        if (!PlayerSpawner.Player.TryGet(out var player))
        {
            this.Log("Player not found", LogType.Error);
            return;
        }
        
        _target = player;
        if (_target)
        {
            if (_target.gameObject.TryGetComponent<Rigidbody>(out var rb))
            {
                _rigidbody = rb;
                
            }

            if (_target.GetModel().TryGetComponent<MotionController>(out var controller))
            {
                _motionController = controller;
            }
        }
        
        _velStat = new RollingWindow(windowSize);
        _horStat = new RollingWindow(windowSize);
        _yStat   = new RollingWindow(windowSize);
#endif
    }

    private void OnGUI()
    {
#if MOVEMENT_TEST_ENABLED
        var defaultRect = new Rect(5, 5, Screen.width, 20);
        
        for (var i = 0; i < logs.Count; i++)
        {
            var rect = new Rect(defaultRect);
            rect.y += (1 + defaultRect.height) * i;

            var style = new GUIStyle()
            {
                fontSize = 20,
                fontStyle = FontStyle.Normal,
            };
            
            style.normal.textColor = Color.white;
            
            GUI.Label(rect, logs[i], style);
        }
#endif
    }

#if MOVEMENT_TEST_ENABLED
    private void FixedUpdate()
    {
        if (_rigidbody)
        {
            var v = _rigidbody.velocity;
            var h = v.XOZ();

            _velMag = v.magnitude;
            _horMag = h.magnitude;
            _yVel = v.y;
            
            _velStat.Add(_velMag);
            _horStat.Add(_horMag);
            _yStat.Add(_yVel);
        }
    }
#endif
    

#if MOVEMENT_TEST_ENABLED
    private void Update()
    {
        logs.Clear();
        
        if (_rigidbody)
        {
            logs.Add("---------------------");
            logs.Add($"Vel Mag: {_velMag:00.0}   (avg {_velStat.Mean:00.0}, min {_velStat.Min:00.0}, max {_velStat.Max:00.0})");
            logs.Add($"Hor Mag: {_horMag:00.0}   (avg {_horStat.Mean:00.0}, min {_horStat.Min:00.0}, max {_horStat.Max:00.0})");
            logs.Add($"Y   Vel: {_yVel:00.0}    (avg {_yStat.Mean:00.0}, min {_yStat.Min:00.0}, max {_yStat.Max:00.0})");
            logs.Add($"Samples: {_velStat.Count}/{windowSize} (FixedUpdate)");
        }

        if (_motionController != null && _motionController.Context != null)
        {
            var context = _motionController.Context;
            
            logs.Add("---------------------");
            logs.Add($"Is grounded: {context.Grounded}");
            logs.Add($"Jump input: {context.Jump} | Is jumping: {context.IsJumping}");
            logs.Add($"Dash input: {context.Dash} | Is dashing: {context.IsDashing}");
        }
        
    }
#endif
}
