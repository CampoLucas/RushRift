using UnityEngine;
using UnityEngine.Serialization;

namespace Game.UI.Screens
{
    public class GameplayView : UIView
    {
        public BarView HeathBar => heathBar;
        public BarView EnergyBar => energyBar;
        
        [SerializeField] private BarView heathBar;
        [SerializeField] private BarView energyBar;
    }
}