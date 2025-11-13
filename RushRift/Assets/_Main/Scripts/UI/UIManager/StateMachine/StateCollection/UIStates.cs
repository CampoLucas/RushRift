using System.Collections.Generic;
using System.Linq;
using Game.UI.StateMachine.Interfaces;
using UnityEngine;

namespace Game.UI.StateMachine
{
    [CreateAssetMenu(menuName = "Game/UI/States")]
    public class UIStates : UIStateCollection
    {
        [SerializeField, HideInInspector] private UIScreen root;
        [SerializeField, HideInInspector] private List<UITransitionDefinition> transitions;

        public override UIScreen GetRootScreen()
        {
            return root;
        }
        
        public override Dictionary<UIScreen, BaseUIPresenter> GetPresenters()
        {
            return presenters.GetDictionary();
        }

        public override List<UITransitionDefinition> GetTransitions()
        {
            return transitions != null ? new List<UITransitionDefinition>(transitions) : new List<UITransitionDefinition>();
        }
    }
}