using System.Collections.Generic;
using Game.UI.StateMachine;
using Game.UI.StateMachine.Interfaces;
using MyTools.Global;
using UnityEngine;

namespace Game.UI.StateMachine
{
    public abstract class UIStateCollection : ScriptableObject
    {
        public List<UITransitionDefinition> Transitions => transitions;
        
        [Header("Presenters")]
        [SerializeField] private SerializedDictionary<UIScreen, BaseUIPresenter> presenters = new();

        [SerializeField, HideInInspector] private List<UITransitionDefinition> transitions;
    }
}