using System.Collections;
using System.Collections.Generic;
using Game.Entities.Components;
using Game.Entities.Enemies.Components;
using UnityEngine;

namespace Game.Entities
{
    [CreateAssetMenu(menuName = "Game/Entities/Enemy/TurretModel")]

    public class TurretModelSO : EnemyModelSO // ToDo: me scriptable component, that way you don't need to create a new model for each different entity
    {
        public override NullCheck<IModel> GetProxy()
        {
            return new NullCheck<IModel>(new EntityModel<TurretModelSO>(this));

        }

        public override void Init(in IController controller, in IModel model)
        {
            var c = controller;
            model.TryAddComponent(EnemyComponentFactory);
            model.TryAddComponent(() => GetComboComponent(c));
            model.TryAddComponent(HealthComponentFactory);
        }
        
        private EnemyComponent EnemyComponentFactory() => new EnemyComponent();
        private HealthComponent HealthComponentFactory() => Health.GetComponent();
    }
}


