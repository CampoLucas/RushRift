using Game.DesignPatterns.Observers;
using Game.Entities.Components;
using UnityEngine;

namespace Game.Entities.AttackSystem
{
    public class BlinkModule : StaticModuleData
    {
        public BlinkConfig BlinkConfig => config;
        [SerializeField] private BlinkConfig config;
        
        public override IModuleProxy GetProxy(IController controller, bool disposeData = false)
        {
            return new BlinkProxy(this, ChildrenProxies(controller), controller);
        }
    }

    public class BlinkProxy : ModuleProxy<BlinkModule>
    {
        private IController _controller;
        private BlinkComponent _blink;

        public BlinkProxy(BlinkModule data, IModuleProxy[] children, IController controller, bool disposeData = false) : base(data, children, disposeData)
        {
            if (controller == null || !controller.Origin) return;
            _controller = controller;

            if (!controller.GetModel().TryAddOrGetComponent(CreateBlinkComponent, out _blink))
            {
                Debug.LogWarning($"WARNING: Couldn't add the [{typeof(BlinkComponent).Name}] to the model.");
            }
        }

        private BlinkComponent CreateBlinkComponent()
        {
            return new BlinkComponent(Data.BlinkConfig, _controller.Origin, Camera.main.transform,
                _controller.Origin.GetComponent<Rigidbody>());
        }

        protected override void BeforeInit()
        {
            StartObserver = new ActionObserver<ModuleParams>(OnStart);
            EndObserver = new ActionObserver<ModuleParams>(OnEnd);
            UpdateObserver = new ActionObserver<ModuleParams, float>(OnUpdate);
        }

        

        private void OnStart(ModuleParams mParams)
        {
            _blink.BeginBlink();
        }
        
        private void OnUpdate(ModuleParams mParams, float delta)
        {
            if (_blink.TryBlink())
            {
                
            }
        }

        private void OnEnd(ModuleParams mParams)
        {
            //_blink.ResetState(false);
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            _controller = null;
            _blink = null;
        }
    }
}