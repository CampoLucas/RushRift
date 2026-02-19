using System;
using System.Collections.Generic;
using Game.Entities.AttackSystem;
using Game.Entities.Components;
using Game.Entities.Components.MotionController;
using Game.InputSystem;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Entities
{
    [CreateAssetMenu(menuName = "Game/Entities/Player/Model")]
    public class PlayerModelSO : EntityModelSO
    {
        public HealthComponentData Health => health;
        public EnergyComponentData Energy => energy;

        // [FormerlySerializedAs("jump")]
        // [Header("Movement Stats Old")]
        // [SerializeField] private JumpData jumpOld;
        // [FormerlySerializedAs("dash")] [SerializeField] private DashData dashOld;

        [Header("Target Detection")]
        [SerializeField] private TargetDetectConfig targetDetection;
        
        [Header("Movement Stats New")]
        [SerializeField] private MovementConfig movement;
        [SerializeField] private AirResistanceConfig airResistance;
        [SerializeField] private JumpConfig jump;
        [SerializeField] private DashConfig dash;
        [SerializeField] private GravityConfig gravity;
        [SerializeField] private GroundDetectionConfig groundDetection;
        [SerializeField] private InputConfig input;

        [Header("Combo")]
        [SerializeField] private Combo comboData;
        
        [Header("Attributes")]
        [SerializeField] private HealthComponentData health;
        [SerializeField] private EnergyComponentData energy;
        
        public ComboHandler GetComboComponent(IController controller)
        {
            return new ComboHandler(controller, comboData, new Dictionary<string, Func<bool>>
            {
                { "Primary", PrimaryAttack },
                { "Light", LightAttack },
                { "Heavy", HeavyAttack },
                { "HeavyCancel", HeavyAttackCancel },
                { "Secondary", SecondaryAttack },
                { "SecondaryTap", SecondaryAttackTap },
                { "SecondaryHold", SecondaryAttackHold },
                { "SecondaryCancel", SecondaryAttackCancel },
                { "Blink", Blink },
            });
        }

        public MotionController GetMotionController(Rigidbody rigidBody, CapsuleCollider collider, Transform orientation, Transform look)
        {
            return new MotionController(rigidBody, collider, orientation, look, new MotionConfig[]
            {
                movement,
                airResistance,
                jump,
                dash,
                gravity,
                groundDetection,
                input,
            });
        }
        
        private bool PrimaryAttack() => InputManager.GetActionPerformed(InputManager.Input.Primary);
        private bool LightAttack() => InputManager.GetActionPerformed(InputManager.Input.PrimaryTap);
        private bool HeavyAttack() => InputManager.GetActionPerformed(InputManager.Input.PrimaryHold);
        private bool HeavyAttackCancel() => InputManager.GetActionCanceled(InputManager.Input.PrimaryHold);
        private bool SecondaryAttack() => InputManager.GetActionPerformed(InputManager.Input.Secondary);
        private bool SecondaryAttackTap() => InputManager.GetActionPerformed(InputManager.Input.SecondaryTap);
        private bool SecondaryAttackHold() => InputManager.GetActionPerformed(InputManager.Input.SecondaryHold);
        private bool SecondaryAttackCancel() => InputManager.GetActionCanceled(InputManager.Input.SecondaryHold);
        private bool Blink() => InputManager.GetActionPerformed(InputManager.Input.Blink);


        public override NullCheck<IModel> GetProxy()
        {
            return new NullCheck<IModel>(new EntityModel<PlayerModelSO>(this));
        }

        public override void Init(in IController controller, in IModel model)
        {
            var playerObject = controller.Origin.gameObject;
            var c = controller;
            if (playerObject.TryGetComponent<Rigidbody>(out var rigidBody) && playerObject.TryGetComponent<CapsuleCollider>(out var collider))
            {
                model.TryAddComponent(() => GetMotionController(rigidBody, collider, c.Origin, c.Joints.GetJoint(EntityJoint.Eyes)));
            }

            model.TryAddComponent(HealthComponentFactory); 
            model.TryAddComponent(EnergyComponentFactory);
            model.TryAddComponent(() => targetDetection.InstantiateComponent(c.Origin, Camera.main.transform));
            model.TryAddComponent(() => GetComboComponent(c));
        }

        private HealthComponent HealthComponentFactory() => Health.GetComponent();
        private EnergyComponent EnergyComponentFactory() => Energy.GetComponent();
        
    }
}