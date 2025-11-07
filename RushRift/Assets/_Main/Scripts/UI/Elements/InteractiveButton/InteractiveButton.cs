using System;
using Game.DesignPatterns.Observers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI.Screens.Elements
{
    [AddComponentMenu("UI/Interactive Button", 31)]
    public sealed class InteractiveButton : Button, ISubject<ButtonSelectState>
    {
        private NullCheck<Subject<ButtonSelectState>> _transitionSubject = new Subject<ButtonSelectState>();
        private bool _isHovered;
        private bool _disposed;
        private SelectionState _prevState = (SelectionState)(-1);

        protected override void Awake()
        {
            _disposed = false;
            base.Awake();
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);

            switch (state)
            {
                case SelectionState.Normal:
                    if (state != _prevState) NotifyAll(ButtonSelectState.Normal);
                    break;
                case SelectionState.Highlighted:
                    if (state != _prevState) NotifyAll(ButtonSelectState.HighlightEnter);
                    break;
                case SelectionState.Pressed:
                    if (state != _prevState) NotifyAll(ButtonSelectState.Pressed);
                    break;
                case SelectionState.Selected:
                    if (state != _prevState) NotifyAll(ButtonSelectState.Selected);
                    break;
                case SelectionState.Disabled:
                    if (state != _prevState) NotifyAll(ButtonSelectState.Disabled);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }

            _prevState = state;
        }
        
        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            NotifyAll(ButtonSelectState.Released);
        }
        

        public bool Attach(DesignPatterns.Observers.IObserver<ButtonSelectState> observer, bool disposeOnDetach = false)
        {
            return _transitionSubject.TryGet(out var subject) && subject.Attach(observer, disposeOnDetach);
        }

        public bool Detach(DesignPatterns.Observers.IObserver<ButtonSelectState> observer)
        {
            return _transitionSubject.TryGet(out var subject) && subject.Detach(observer);
        }

        public void DetachAll()
        {
            if (_transitionSubject.TryGet(out var subject))
            {
                subject.DetachAll();
            }
        }

        public void NotifyAll(ButtonSelectState state)
        {
            if (state == ButtonSelectState.HighlightEnter)
            {
                _isHovered = true;
            }
            else if (_isHovered)
            {
                _isHovered = false;
                NotifyTransition(ButtonSelectState.HighlightExit);
            }
            
            NotifyTransition(state);
        }
        
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_transitionSubject.TryGet(out var subject))
            {
                subject.DetachAll();
                subject.Dispose();
            }
        }
        
        protected override void OnDestroy()
        {
            Dispose();
            base.OnDestroy();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (transition != Transition.None)
                transition = Transition.None;
        }
#endif
        
        private void NotifyTransition(ButtonSelectState state)
        {
            if (_transitionSubject.TryGet(out var subject))
            {
                subject.NotifyAll(state);
            }
        }
    }

    public enum ButtonSelectState
    {
        /// <summary>
        /// The UI object can be selected.
        /// </summary>
        Normal,

        /// <summary>
        /// The UI object is highlighted.
        /// </summary>
        HighlightEnter,
        HighlightExit,

        /// <summary>
        /// The UI object is pressed.
        /// </summary>
        Pressed,

        /// <summary>
        /// The UI object is selected
        /// </summary>
        Selected,

        /// <summary>
        /// The UI object cannot be selected.
        /// </summary>
        Disabled,
        Released,
    }
}