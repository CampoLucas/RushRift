using UnityEngine;

namespace Game.UI.Screens
{
    public class GameplayView : UIView
    {
        public BarView HeathBar => heathBar;
        public BarView StaminaBar => staminaBar;
        public BarView ManaBar => manaBar;
        
        [SerializeField] private BarView heathBar;
        [SerializeField] private BarView staminaBar;
        [SerializeField] private BarView manaBar;
    }
}