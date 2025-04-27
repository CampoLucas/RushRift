using UnityEngine;

namespace Game.UI.Screens
{
    public class GameplayView : UIView
    {
        public AttributeBarView HeathBar => heathBar;
        public AttributeBarView StaminaBar => staminaBar;
        public AttributeBarView ManaBar => manaBar;
        
        [SerializeField] private AttributeBarView heathBar;
        [SerializeField] private AttributeBarView staminaBar;
        [SerializeField] private AttributeBarView manaBar;
    }
}