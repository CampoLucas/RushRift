using System.Linq;
using UnityEngine;

namespace Game.Entities.AttackSystem
{
    public class PlayAnimation : StaticModuleData
    {
        public string Animation => animation;
        public float Delay => delay;
        public float NormalizedTime => useNormalizedTime ? ((float)startFrame / framesPerSecond) / animationDuration : 0;
        
        [Header("Animation")]
        [SerializeField] private string animation;
        
        [Header("Normalize Time")]
        [SerializeField] private bool useNormalizedTime;
        [SerializeField] private int layer;
        [SerializeField] private int startFrame = 0;
        [SerializeField] private int framesPerSecond = 24;
        [SerializeField] private float animationDuration;

        [Header("Settings")]
        [Tooltip("To play the animation at the end of the module execution needs to be less than 0")]
        [SerializeField] private float delay;
        
        
        public override IModuleProxy GetProxy(IController controller, bool disposeData = false)
        {
            return new PlayAnimationProxy(this, controller, Children.Select(c => GetProxy(controller)).ToArray());
        }

        public void Play(IView view)
        {
            if (useNormalizedTime)
            {
                view.Play(animation, layer, NormalizedTime);
            }
            else
            {
                view.Play(animation);
            }
        }
    }
}