using Game.Detection;
using UnityEngine;

namespace Game.Entities.Components
{
    [System.Serializable]
    public class LaserComponentData
    {
        public bool StartOn => startOn;
        public bool DestroyDestroyables => destroyDestroyables;
        
        [Header("Settings")]
        [SerializeField] private bool startOn = true;
        [Tooltip("Flag to enable the laser to destroy walls, objects, etc. that the DamageDash upgrade can destroy.")]
        [SerializeField] private bool destroyDestroyables = false;
        
        [Header("Detection")]
        [SerializeField] private LineDetectData detection;

        public LineDetect GetDetection(Transform origin)
        {
            return detection.Get(origin);
        }
    }
}