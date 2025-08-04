using System;
using System.Collections.Generic;
using Game.Entities.AttackSystem;
using Game.Entities.Components;
using Game.Entities.Components.MotionController;
using Game.Inputs;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Entities
{
    [CreateAssetMenu(menuName = "Game/Entities/Player/Model")]
    public class PlayerModelSO : EntityModelSO
    {
        public JumpData Jump => jumpOld;
        public HealthComponentData Health => health;
        public EnergyComponentData Energy => energy;
        public DashData DashOld => dashOld;

        [FormerlySerializedAs("jump")]
        [Header("Movement Stats Old")]
        [SerializeField] private JumpData jumpOld;
        [FormerlySerializedAs("dash")] [SerializeField] private DashData dashOld;

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
        
        private bool PrimaryAttack() => InputManager.GetActionPerformed(InputManager.PrimaryAttackInput);
        private bool LightAttack() => InputManager.GetActionPerformed(InputManager.PrimaryAttackTapInput);
        private bool HeavyAttack() => InputManager.GetActionPerformed(InputManager.PrimaryAttackHoldInput);
        private bool HeavyAttackCancel() => InputManager.GetActionCanceled(InputManager.PrimaryAttackHoldInput);
        private bool SecondaryAttack() => InputManager.GetActionPerformed(InputManager.SecondaryAttackInput);
        
        public override NullCheck<IModel> GetProxy()
        {
            return new NullCheck<IModel>(new PlayerModel(this));
        }
    }
}