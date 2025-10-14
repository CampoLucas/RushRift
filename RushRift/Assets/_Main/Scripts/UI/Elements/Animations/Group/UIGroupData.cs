using UnityEngine;

namespace Game.UI.Group
{
    [System.Serializable]
    public struct UIGroupData
    {
        public UIAnimation Animation => animation;
        public float Delay => delay;
        
        [SerializeField] private UIAnimation animation;
        [SerializeField] private float delay;
    }
}