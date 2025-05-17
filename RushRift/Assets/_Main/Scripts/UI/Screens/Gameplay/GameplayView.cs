using UnityEngine;
using UnityEngine.Serialization;

namespace Game.UI.Screens
{
    public class GameplayView : UIView
    {
        public BarView HeathBar => heathBar;
        public BarView EnergyBar => energyBar;
        public BarView ManaBar => manaBar;
        
        [SerializeField] private BarView heathBar;
        [FormerlySerializedAs("staminaBar")] [SerializeField] private BarView energyBar;
        [SerializeField] private BarView manaBar;
    }
}