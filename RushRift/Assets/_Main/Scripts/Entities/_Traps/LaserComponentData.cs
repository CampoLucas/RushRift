using Game.Detection;
using UnityEngine;

namespace Game.Entities.Components
{
    [System.Serializable]
    public class LaserComponentData
    {
        [Header("Detection")]
        [SerializeField] private LineDetectData detection;

        public LineDetect GetDetection(Transform origin)
        {
            return detection.Get(origin);
        }
    }
}