using UnityEngine;

namespace Game.Entities.Components.MotionController.Strategies
{
    public abstract class DashDirStrategy<TConfig> : IDashDirStrategy
        where TConfig : DashDirConfig
    {
        protected TConfig Config;

        protected DashDirStrategy(TConfig config)
        {
            Config = config;
        }

        public Vector3 GetDir(in MotionContext context, in DashConfig config)
        {
            return OnGetDir(context, config) * Config.Weight;
        }
        
        public void Dispose()
        {
            OnDispose();
            Config = null;
        }

        protected abstract Vector3 OnGetDir(in MotionContext context, in DashConfig config);
        protected virtual void OnDispose() { }
    }
}