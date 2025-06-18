using Unity.VisualScripting;
using UnityEngine;

namespace Game.UI.Screens
{
    /// <summary>
    /// The logic that visually fades in and out a screen when transitioning.
    /// </summary>
    public class UIEffectTransition
    {
        private NullCheck<UIState> _from;
        private NullCheck<UIState> _to;
        private NullCheck<UIEffect> _effect;
        
        private readonly float _duration;
        private readonly float _fadeOutTime;
        private readonly float _fadeOutStartTime;
        private readonly float _fadeInTime;
        private readonly float _fadeInStartTime;


        private float _effectStartTime;
        private bool _effectStarted;
        private bool _effectEnded;

        private float _timer;

        public UIEffectTransition(UIState from, UIState to, float outTime, float outStartTime, float inTime, float inStartTime)
        {
            _from.Set(from);
            _to.Set(to);

            _duration = (outStartTime + outTime) - ((outStartTime + outTime) - (inStartTime + inTime));
            _fadeOutTime = outTime;
            _fadeOutStartTime = outStartTime;
            _fadeInTime = inTime;
            _fadeInStartTime = inStartTime;
            //_effect.Set(effect);
        }

        public bool DoTransition(float delta)
        {
            _timer += delta;
            if (_from.TryGetValue(out var fromState))
            {
                fromState.FadeOut(_timer, _fadeOutStartTime, _fadeOutTime);
            }

            if (_effect.TryGetValue(out var trEffect))
            {
                
            }

            if (_to.TryGetValue(out var toState))
            {
                toState.FadeIn(_timer, _fadeInStartTime, _fadeInTime);
            }

            return _timer >= _duration;
        }
        
    }
}