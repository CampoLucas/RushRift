using System;
using System.Collections.Generic;
using Game.Entities.AttackSystem;
using Game.Entities.Components;
using Game.Entities.Enemies.Components;
using Game.Inputs;
using UnityEngine;

namespace Game.Entities
{
    [CreateAssetMenu(menuName = "Game/Entities/Enemy/Model")]
    public class EnemyModel : EntityModelSO
    {
        public MovementData Movement => movement;
        public GravityData Gravity => gravity;
        public Combo Combo => combo;
        public HealthComponentData Health => health;
        public StaminaComponentData Stamina => stamina;
        public ManaComponentData Mana => mana;
        
        [Header("Movement")]
        [SerializeField] private MovementData movement;
        [SerializeField] private GravityData gravity;

        [Header("Combo")]
        [SerializeField] private Combo combo;
        
        [Header("Attributes")]
        [SerializeField] private HealthComponentData health;
        [SerializeField] private StaminaComponentData stamina;
        [SerializeField] private ManaComponentData mana;
        
        public override NullCheck<IModel> GetProxy()
        {
            return new NullCheck<IModel>(new EnemyModelProxy(this));
        }
        
        public ComboHandler GetComboComponent(IController controller)
        {
            return new ComboHandler(controller, combo, new Dictionary<string, Func<bool>>
            {
                { "Light", NoAttack },
                { "Heavy", NoAttack },
            });
        }

        private bool NoAttack() => false;
    }

    public class EnemyModelProxy : EntityModel<EnemyModel>
    {
        public EnemyModelProxy(EnemyModel data) : base(data)
        {
        }

        public override void Init(IController controller)
        {
            TryAddComponent(new EnemyComponent());
            
            if (controller.Origin.gameObject.TryGetComponent<CharacterController>(out var characterController))
            {
                TryAddComponent(Data.Movement.GetMovement(characterController));
            }
            
            TryAddComponent(Data.GetComboComponent(controller));
            TryAddComponent(Data.Health.GetComponent()); 
            TryAddComponent(Data.Stamina.GetComponent());
            TryAddComponent(Data.Mana.GetComponent());
        }
    }
}