using System;
using System.Collections.Generic;
using Game.Entities.AttackSystem;
using Game.Entities.Components;
using Game.Entities.Enemies.Components;
using Game.InputSystem;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Entities
{
    [CreateAssetMenu(menuName = "Game/Entities/Enemy/Model")]
    public class EnemyModelSO : EntityModelSO
    {
        [Header("Movement")]
        [SerializeField] private MovementData movement;
        [SerializeField] private GravityData gravity;

        [Header("Combo")]
        [SerializeField] private Combo combo;
        
        [FormerlySerializedAs("health")]
        [Header("Attributes")]
        [SerializeField] protected HealthComponentData Health;
        [FormerlySerializedAs("stamina")] [SerializeField] private EnergyComponentData energy;
        [SerializeField] private ManaComponentData mana;
        
        public override NullCheck<IModel> GetProxy()
        {
            return new NullCheck<IModel>(new EntityModel<EnemyModelSO>(this));
        }

        public override void Init(in IController controller, in IModel model)
        {
            model.TryAddComponent(EnemyComponentFactory);
            
            if (controller.Origin.gameObject.TryGetComponent<CharacterController>(out var characterController))
            {
                model.TryAddComponent(() => movement.GetMovement(characterController));
            }

            var c = controller;
            model.TryAddComponent(() => GetComboComponent(c));
            model.TryAddComponent(HealthComponentFactory); 
            //model.TryAddComponent(energy.GetComponent());
            //model.TryAddComponent(mana.GetComponent());
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
        private EnemyComponent EnemyComponentFactory() => new EnemyComponent();
        private HealthComponent HealthComponentFactory() => Health.GetComponent();
    }
}