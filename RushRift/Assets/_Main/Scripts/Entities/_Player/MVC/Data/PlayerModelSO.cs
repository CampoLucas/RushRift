using System;
using System.Collections.Generic;
using Game.Entities.AttackSystem;
using Game.Entities.Components;
using Game.Inputs;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Entities
{
    [CreateAssetMenu(menuName = "Game/Entities/Player/Model")]
    public class PlayerModelSO : EntityModelSO
    {
        public PlayerMovementData PlayerMovement => playerMovement;
        public JumpData Jump => jump;
        public HealthComponentData Health => health;
        public EnergyComponentData Energy => energy;
        public DashData Dash => dash;

        [Header("Movement Stats")]
        [SerializeField] private PlayerMovementData playerMovement;
        [SerializeField] private JumpData jump;
        [SerializeField] private DashData dash;

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