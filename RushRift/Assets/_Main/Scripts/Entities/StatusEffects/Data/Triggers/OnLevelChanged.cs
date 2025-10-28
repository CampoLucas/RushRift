namespace Game.Entities
{
    public class OnLevelChanged : EffectTrigger
    {
        public override Trigger GetTrigger(IController controller)
        {
#if false
            var targetSubject = GameEntry.LoadingState.LevelChanged;
            return new Trigger(targetSubject, this, true);
#else
            return Trigger(controller, this);
#endif
        }

        public override bool Evaluate(ref IController args)
        {
            return GameEntry.LoadingState.Loading;
        }

        public static Trigger Trigger(IController controller, IPredicate<IController> predicate)
        {
            var targetSubject = GameEntry.LoadingState.LevelChanged;
            return new Trigger(targetSubject, predicate, true);
        }

        public class IsLoadingPredicate : IPredicate<IController>
        {
            public bool Evaluate(ref IController args)
            {
                return GameEntry.LoadingState.Loading;
            }

            public void Dispose()
            {
                
            }
        }
    }
}