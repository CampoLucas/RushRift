using UnityEngine;

namespace Game.UI.StateMachine
{
    [CreateAssetMenu(menuName = "Game/UI/StatesOverride")]
    public class UIStatesOverride : UIStateCollection
    {
        public UIStateCollection Parent => parent;

        [SerializeField] private UIStateCollection parent;
    }
}