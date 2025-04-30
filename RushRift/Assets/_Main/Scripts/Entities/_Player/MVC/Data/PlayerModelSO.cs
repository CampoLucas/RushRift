using System;
using System.Collections.Generic;
using Game.Entities.AttackSystem;
using Game.Entities.Components;
using Game.Inputs;
using UnityEngine;

namespace Game.Entities
{
    [CreateAssetMenu(menuName = "Game/Entities/Player/Model")]
    public class PlayerModelSO : EntityModelSO
    {
        public MovementData MoveSpeed => moveSpeed;
        public JumpData Jump => jump;
        public GravityData Gravity => gravity;
        public HealthComponentData Health => health;
        public StaminaComponentData Stamina => stamina;
        public ManaComponentData Mana => mana;
        
        [Header("Movement Stats")] 
        [SerializeField] private MovementData moveSpeed;
        [SerializeField] private JumpData jump;
        [SerializeField] private GravityData gravity;

        [Header("Combo")]
        [SerializeField] private Combo comboData;
        
        [Header("Attributes")]
        [SerializeField] private HealthComponentData health;
        [SerializeField] private StaminaComponentData stamina;
        [SerializeField] private ManaComponentData mana;
        
        public ComboHandler GetComboComponent(IController controller)
        {
            return new ComboHandler(controller, comboData, new Dictionary<string, Func<bool>>
            {
                { "Light", LightAttack },
                { "Heavy", HeavyAttack },
                { "HeavyCancel", HeavyAttackCancel },
                { "Secondary", SecondaryAttack },
            });
        }
        
        private bool LightAttack() => InputManager.GetActionPerformed(InputManager.LightAttackInput);
        private bool HeavyAttack() => InputManager.GetActionPerformed(InputManager.HeavyAttackInput);
        private bool HeavyAttackCancel() => InputManager.GetActionCanceled(InputManager.HeavyAttackInput);
        private bool SecondaryAttack() => InputManager.GetActionPerformed(InputManager.SecondaryAttackInput);
        
        public override NullCheck<IModel> GetProxy()
        {
            return new NullCheck<IModel>(new PlayerModel(this));
        }
    }
}