using Game.DesignPatterns.Observers;
using Game.Entities.Components;
using Game.Entities.Components.MotionController;
using MyTools.Global;
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
        private NullCheck<BlinkComponent> _blink;
        private NullCheck<EnergyComponent> _energy;

        public BlinkProxy(BlinkModule data, IModuleProxy[] children, IController controller, bool disposeData = false) : base(data, children, disposeData)
        {
            if (controller == null || !controller.Origin) return;
            _controller = controller;

            if (!controller.GetModel().TryAddOrGetComponent(CreateBlinkComponent, out var blink))
            {
                Debug.LogWarning($"WARNING: Couldn't add the [{nameof(BlinkComponent)}] to the model.");
            }

            _blink = blink;
        }

        private BlinkComponent CreateBlinkComponent()
        {
            if (_controller.GetModel().TryGetComponent<TargetDetectComp>(out var detectComp))
            {
                _controller.GetModel().TryGetComponent<EnergyComponent>(out var energyComp);
                return new BlinkComponent(Data.BlinkConfig, detectComp, energyComp);
            }
            
            this.Log("No Target Detector detected...", logType: LogType.Error);
            return null;
        }

        protected override void BeforeInit()
        {
            StartObserver = new ActionObserver<ModuleParams>(OnStart);
            EndObserver = new ActionObserver<ModuleParams>(OnEnd);
            UpdateObserver = new ActionObserver<ModuleParams, float>(OnUpdate);
        }
        
        private void OnStart(ModuleParams mParams)
        {
            if (!_blink.TryGet(out var blink)) return;
            blink.BeginCharge();
        }
        
        private void OnUpdate(ModuleParams mParams, float delta)
        {
            if (!_blink.TryGet(out var blink)) return;
            if (blink.State != BlinkComponent.BlinkState.Charged) return;
            
            if (mParams.Owner.TryGet(out var controller) && 
                ExecuteBlink(controller) &&
                _energy.TryGet(out var energy, GetEnergyComponent))
            {
                blink.IncreaseExecutedCount();
                energy.Decrease(energy.Value);
            }
                
            blink.FinishCharge();
        }

        private EnergyComponent GetEnergyComponent()
        {
            if (_controller.GetModel().TryGetComponent<EnergyComponent>(out var energy))
            {
                return energy;
            }

            return null;
        }

        private void OnEnd(ModuleParams mParams)
        {
            if (!_blink.TryGet(out var blink)) return;
            blink.CancelCharge(false);
            //_blink.ResetState(false);
        }
        
        private bool ExecuteBlink(IController controller)
        {
            if (!_blink.TryGet(out var blink)) return false;
            var origin = blink.Origin;
            var motion = new NullCheck<MotionController>();
            if (!blink.CurrentTarget.TryGet(out var target) || !origin ||
                !blink.TryGetBlinkCoords(out var blinkPos, out var blinkRot)) return false;

            origin.position = blinkPos;
            origin.rotation = blinkRot;

            if (Data.BlinkConfig.ZeroVelocity &&
                controller.GetModel().TryGetComponent<MotionController>(out var motionController))
            {
                motion.Set(motionController);
                motionController.Context.Velocity = Vector3.zero;
            }

            blink.ConfirmCooldown(Data.BlinkConfig.Cooldown);
            
            if (Data.BlinkConfig.KillOnBlink)
                KillTarget(target, motion);
            
            return true;
        }
        
        private void KillTarget(NullCheck<Transform> t, NullCheck<MotionController> motion)
        {
            if (!t.TryGet(out var target)) return;

            // var controller = target.GetComponentInParent<EntityController>();
            // if (controller == null) return;
            
            var controller = target.GetComponentInParent<EntityController>();
            var barrel = target.GetComponentInParent<ExplosiveBarrel>();
            if (barrel && motion.TryGet(out var m) && m.Body.TryGet(out var rb))
                barrel.TriggerExplosionExternal(null, true, rb);

            if (controller != null)
            {
                var model = controller.GetModel();
                if (model != null && model.TryGetComponent<HealthComponent>(out var health))
                {
                    health.Intakill(target.position);
                    return;
                }
                controller.OnNotify(EntityController.DESTROY);
                
            }

        }

        protected override void OnDispose()
        {
            base.OnDispose();
            _controller = null;
            _blink = null;
        }
    }
}