using UnityEngine;

namespace Game.UI.Screens
{
    public class UITransition
    {
        public UIScreen To { get; private set; }
        
        private IPredicate _condition;
        private readonly float _fadeout;
        private readonly float _fadeIn;
        private readonly float _fadeInStart;
        //private readonly bool _doesFadeTransition;

        public UITransition(UIScreen to, IPredicate condition, float fadeOut, float fadeIn, float fadeInStartTime)
        {
            To = to;
            _condition = condition;

            _fadeout = fadeOut;
            _fadeIn = fadeIn;
            _fadeInStart = fadeInStartTime;

            //_doesFadeTransition = fadeIn > 0 || fadeOut > 0;
        }

        public void Do(UIStateMachine stateMachine)
        {
            stateMachine.TransitionTo(To, _fadeout, _fadeIn, _fadeInStart);
            // if (_doesFadeTransition) stateMachine.TransitionTo(To, _fadeout, _fadeIn, _fadeInStart);
            // else stateMachine.TryChangeState(To);

        }

        public bool Evaluate()
        {
            return _condition?.Evaluate() ?? false;
        }

        public void SetTransition(UIScreen to)
        {
            To = to;
        }
        
        public void Dispose()
        {
            _condition?.Dispose();
            _condition = null;
        }
    }
}