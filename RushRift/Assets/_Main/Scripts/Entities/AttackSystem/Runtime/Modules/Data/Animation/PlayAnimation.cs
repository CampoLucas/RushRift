using System.Linq;
using UnityEngine;

namespace Game.Entities.AttackSystem.Modules
{
    public class PlayAnimation : StaticModuleData
    {
        public string Animation => animation;
        public float Delay => delay;
        
        [Header("Animation")]
        [SerializeField] private string animation;

        [Header("Settings")]
        [Tooltip("To play the animation at the end of the module execution needs to be less than 0")]
        [SerializeField] private float delay;
        
        
        public override IModuleProxy GetProxy(IController controller, bool disposeData = false)
        {
            return new PlayAnimationProxy(this, controller, Children.Select(c => GetProxy(controller)).ToArray());
        }

        
    }
}