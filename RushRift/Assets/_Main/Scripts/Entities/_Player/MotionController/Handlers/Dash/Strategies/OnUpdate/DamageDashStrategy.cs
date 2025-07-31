namespace Game.Entities.Components.MotionController.Strategies
{
    public class DamageDashStrategy : IDashUpdateStrategy
    {
        public bool OnDashUpdate(in MotionContext context, in float delta)
        {
            return false;
        }
        
        public void Dispose()
        {
            
        }
    }
}