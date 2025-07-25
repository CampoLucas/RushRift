namespace Game.Entities.Components.MotionController
{
    public class MotionHandler<TConfig> : BaseMotionHandler 
        where TConfig : MotionConfig
    {
        protected TConfig Config;

        protected MotionHandler(TConfig config)
        {
            Config = config;
        }

        public override void Dispose()
        {
            Config = null;
        }
    }
}