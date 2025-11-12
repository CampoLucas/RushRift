using System.Collections.Generic;
using Game.UI.StateMachine;
using Game.UI.StateMachine.Interfaces;
using MyTools.Global;
using UnityEngine;

namespace Game.UI.StateMachine
{
    public abstract class UIStateCollection : ScriptableObject
    {
        public SerializedDictionary<UIScreen, BaseUIPresenter> Presenters => presenters;
        
        [Header("Presenters")]
        [SerializeField] protected SerializedDictionary<UIScreen, BaseUIPresenter> presenters = new();

        public abstract UIScreen GetRootScreen();
        /// <summary>
        /// Returns the presenters prefabs.
        /// </summary>
        public abstract Dictionary<UIScreen, BaseUIPresenter> GetPresenters();
        /// <summary>
        /// Returns the transitions for this collection.
        /// </summary>
        public abstract List<UITransitionDefinition> GetTransitions();
        /// <summary>
        /// Returns true if a presenter prefab exist for this screen.
        /// </summary>
        public bool TryGetPresenter(UIScreen screen, out BaseUIPresenter presenter)
        {
            return presenters.TryGetValue(screen, out presenter) && presenter != null;
        }

        public virtual void Test() { }
    }
}