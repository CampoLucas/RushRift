using System;
using UnityEngine;

namespace Game.Detection
{
    public class BoxOverlapDetect : IDisposable
    {
        public bool IsColliding => _isColliding;
        public int Overlaps => _overlaps;
        
        private BoxOverlapDetectData _data;
        private Transform _origin;

        private int _overlaps;
        private Collider[] _colliders;
        private bool _isColliding; // Memoization
        
        public BoxOverlapDetect(Transform origin, BoxOverlapDetectData data)
        {
            _origin = origin;
            _data = data;
            _colliders = new Collider[data.MaxCollisions];
        }

        public bool Detect()
        {
            _overlaps = _data.Detect(_origin, ref _colliders);
            _isColliding = _overlaps > 0;
            return _isColliding;
        }

        public void Dispose()
        {
            _data = null;
        }

        public void Draw(Transform origin, Color color)
        {
            _data.Draw(origin, color);
        }
    }
}