using UnityEngine;

namespace Game.Detection
{
    public class OverlapDetect : IDetection
    {
        public bool IsColliding => _isColliding;
        public int Overlaps => _overlaps;
        public Collider[] Collisions => _colliders;
        
        private IDetectionData _data;
        private Transform _origin;

        private int _overlaps;
        private Collider[] _colliders;
        private bool _isColliding; // Memoization
        
        public OverlapDetect(Transform origin, IDetectionData data)
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
            _origin = null;
            _colliders = null;
        }

        public void Draw(Transform origin, Color color)
        {
            _data.Draw(origin, color);
        }
    }
}