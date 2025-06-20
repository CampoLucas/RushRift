using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.UI.Screens
{
    public class UITransition
    {
        private IPredicate _condition;
        
        protected UITransition(IPredicate condition)
        {
            _condition = condition;
        }

        public virtual void Do(UIStateMachine stateMachine)
        {
            
        }

        public bool Evaluate()
        {
            return _condition?.Evaluate() ?? false;
        }
        
        public void Dispose()
        {
            _condition?.Dispose();
            _condition = null;
        }
    }

    public class UIScreenTransition : UITransition
    {
        private readonly UIScreen _to;
        private readonly float _fadeout;
        private readonly float _fadeIn;
        private readonly float _fadeInStart;
        
        public UIScreenTransition(UIScreen to, IPredicate condition, float fadeOut, float fadeIn, float fadeInStartTime) : base(condition)
        {
            _to = to;
            _fadeout = fadeOut;
            _fadeIn = fadeIn;
            _fadeInStart = fadeInStartTime;
        }
        
        public override void Do(UIStateMachine stateMachine)
        {
            stateMachine.TransitionTo(_to, _fadeout, _fadeIn, _fadeInStart);
        }
    }

    public class UISceneTransition : UITransition
    {
        private readonly SceneTransition _sceneTransition;
        private readonly string _name;

        public UISceneTransition(string name, IPredicate condition) : base(condition)
        {
            _sceneTransition = SceneTransition.Name;
            _name = name;
        }

        public UISceneTransition(SceneTransition sceneTransition, IPredicate condition) : base(condition)
        {
            _sceneTransition = sceneTransition;
        }

        public override void Do(UIStateMachine stateMachine)
        {
            switch (_sceneTransition)
            {
                case SceneTransition.Current:
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                    break;
                case SceneTransition.Next:
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
                    break;
                case SceneTransition.Previous:
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
                    break;
                case SceneTransition.Name:
                    SceneManager.LoadScene(_name);
                    break;
                case SceneTransition.First:
                    SceneManager.LoadScene(0);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public enum SceneTransition
    {
        Current, Next, Previous, Name, First
    }
}