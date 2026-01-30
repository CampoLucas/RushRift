using Game.Entities.Components;
using Game.Entities.Components.MotionController;
using MyTools.Global;
using UnityEngine;

namespace Game.Entities.AttackSystem
{
    [CreateAssetMenu(menuName = "Game/AttackSystem/Predicates/Blink/CanBlink")]
    public class CanBlink : ComboPredicate
    {
        protected override bool OnEvaluate(ComboHandler combo, IAttack next)
        {
            var owner = combo.Owner;
            if (owner == null)
            {
                this.Log("The owner is null...", LogType.Error);
                return false;
            }

            if (!owner.GetModel().TryGetComponent<BlinkComponent>(out var blink))
            {
                this.Log("The controller doesn't have a BlinkComponent", LogType.Error);
                return false;
            }

            if (blink.HasTarget)
            {
                this.Log("It can blink");

                return true;
            }

            this.Log("It can't blink");
            return false;
        }
    }
}