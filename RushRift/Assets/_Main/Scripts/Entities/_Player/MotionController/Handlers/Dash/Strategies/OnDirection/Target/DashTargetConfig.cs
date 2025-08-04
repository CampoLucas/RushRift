using Game.Entities.Enemies.Components;
using UnityEngine;

namespace Game.Entities.Components.MotionController.Strategies
{
    [System.Serializable]
    public class DashTargetConfig : DashDirConfig
    {
        public LayerMask Layer => layer;
        public float Range => range;
        public int MaxColliders => maxColliders;
        public FOVBuilder FovBuilder => fovBuilder;
        
        [Header("Detection")]
        [SerializeField] private LayerMask layer;
        [SerializeField] private float range = 5;
        [SerializeField] private int maxColliders = 5;

        [Header("FOV")]
        [SerializeField] private FOVBuilder fovBuilder;
    }
}