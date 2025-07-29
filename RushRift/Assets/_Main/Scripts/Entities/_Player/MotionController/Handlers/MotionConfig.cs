using UnityEngine;

namespace Game.Entities.Components.MotionController
{
    [System.Serializable]
    public abstract class MotionConfig
    {
        public bool Enabled => enabled;
        public int Order => order;
        
        [Header("Settings")]
        [SerializeField] private bool enabled = true;
        [SerializeField] private int order;

        public abstract void AddHandler(in MotionController controller, in bool rebuildHandlers);
    }
}