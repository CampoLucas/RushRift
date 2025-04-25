using Game.DesignPatterns.Observers;
using UnityEngine;

namespace Game.Entities.AttackSystem.Modules
{
    public class ComboModuleRunner : StaticModuleData
    {
        public override IModuleProxy GetProxy(IController controller, bool disposeData)
        {
            return new ComboModuleRunnerProxy(this, ChildrenProxies(controller), controller, disposeData);
        }
    }

    public class ComboModuleRunnerProxy : ModuleProxy<ComboModuleRunner>
    {
        private IController _controller;
        private NullCheck<ComboHandler> _handler;
        private int _index;
        private int _executions;
        
        public ComboModuleRunnerProxy(ComboModuleRunner data, IModuleProxy[] children, IController controller, bool disposeData = false) : base(data, children, disposeData)
        {
            _controller = controller;
            if (_controller.GetModel().TryGetComponent<ComboHandler>(out var handler))
            {
                _handler = handler;
            }
        }

        protected override void BeforeInit()
        {
            StartObserver = new ActionObserver<ModuleParams>(Start);
        }

        private void Start(ModuleParams args)
        {
            if (!_handler)
            {
                if (_controller.GetModel().TryGetComponent<ComboHandler>(out var handler))
                {
                    _handler = handler;
                }
            }

            _index = 0;
            _executions = 0;
            var proxies = _handler.Get().ComboProxies;
            
            if (proxies == null || proxies.Count == 0) return;
            
            for (var i = 0; i < proxies.Count; i++)
            {
                var proxy = proxies[i];
                proxy.Reset();
            }
        }

        protected override bool FinishedExecuting(ModuleParams mParams, float delta)
        {
            if (!_handler) return true;

            var proxies = _handler.Get().ComboProxies;
            var finished = ExecuteChildren(ref proxies, ref _index, mParams, delta);

            if (finished && _executions < _handler.Get().ComboStats.MultiShotCount)
            {
                _index = 0;
                _executions++;
                
                for (var i = 0; i < proxies.Count; i++)
                {
                    var proxy = proxies[i];
                    proxy.Reset();
                }
                
                return false;
            }
            
            return finished;

        }

        protected override void OnDispose()
        {
            _controller = null;
            _handler = null;
        }
    }
}