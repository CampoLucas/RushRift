using Game.DesignPatterns.Observers;
using UnityEngine;

namespace Game.Entities.AttackSystem
{
    public class PlayAnimationProxy : ModuleProxy<PlayAnimation>
    {
        private IController _controller;
        private bool _executed;
        public PlayAnimationProxy(PlayAnimation data, IController controller, IModuleProxy[] children) : base(data, children)
        {
            _controller = controller;

            switch (Data.Delay)
            {
                case 0:
                    StartObserver = new ActionObserver<ModuleParams>(OnDo);
                    break;
                case > 0:
                    StartObserver = new ActionObserver<ModuleParams>(OnReset);
                    UpdateObserver = new ActionObserver<ModuleParams, float>(OnUpdate);
                    break;
                case < 0:
                    EndObserver = new ActionObserver<ModuleParams>(OnDo);
                    break;
            }
        }
        
        protected override void OnDispose()
        {
            _controller = null;
        }
        
        private void OnReset(ModuleParams mParams)
        {
            _executed = false;
        }

        private void OnUpdate(ModuleParams mParams, float delta)
        {
            if (_executed) return;
            if (Timer < Data.Delay) return;

            _executed = true;
            OnDo(mParams);
        }
        
        private void OnDo(ModuleParams mParams)
        {
            Data.Play(_controller.GetView());
            
            //_controller.GetView().Play(Data.Animation);
        }

    }
}