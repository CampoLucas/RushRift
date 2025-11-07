using Game.DesignPatterns.Observers;

namespace Game.Entities
{
    public class OnValueChanged : AttributeEffectTrigger
    {
        public override Trigger GetTrigger(IController controller)
        {
            if (TryGetAttribute(controller, out var atr))
            {
                return new Trigger(atr.OnValueChanged.ConvertToSimple(), this);
            }

            return null;
        }

        public override bool Evaluate(ref IController args)
        {
            return false;
        }
    }
}