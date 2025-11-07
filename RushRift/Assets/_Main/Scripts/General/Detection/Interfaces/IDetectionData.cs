using UnityEngine;

namespace Game.Detection
{
    public interface IDetectionData
    {
        int MaxCollisions { get; }
        IDetection Get(Transform origin);
        int Detect(Transform origin, ref Collider[] colliders);
        void Draw(Transform origin, Color color);
    }
}