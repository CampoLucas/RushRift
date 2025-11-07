using UnityEngine;

namespace Game.Detection
{
    public class LineDetect
    {
        public bool IsOverlapping => _isOverlapping;
        public int Overlaps => _overlaps;
        public RaycastHit[] Hits => _hits;

        private LineDetectData _data;
        private Transform _origin;
        
        private int _overlaps;
        private RaycastHit[] _hits;
        private bool _isOverlapping;

        public LineDetect(Transform origin, LineDetectData data)
        {
            _origin = origin;
            _data = data;
            _hits = new RaycastHit[data.MaxOverlaps];
        }
        
        public bool Detect(out Vector3 endPos, out bool blocked, out RaycastHit blockHit)
        {
            _overlaps = _data.Detect(_origin, ref _hits, out endPos, out blocked, out blockHit);
            _isOverlapping = _overlaps > 0;
            return _isOverlapping;
        }
        
        public void Dispose()
        {
            _data = null;
            _origin = null;
            _hits = null;
        }

        public void Draw(Transform origin, Color color)
        {
            _data.Draw(origin, color);
        }
    }
}