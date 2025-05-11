using System.Collections;
using System.Collections.Generic;
using Game.Entities.Enemies.Components;
using UnityEngine;

namespace Game.Entities
{
    [CreateAssetMenu(menuName = "Game/Entities/Enemy/TurretModel")]

    public class TurretModel : EnemyModel // ToDo: me scriptable component, that way you don't need to create a new model for each different entity
    {
        public override NullCheck<IModel> GetProxy()
        {
            return new NullCheck<IModel>(new TurretModelProxy(this));

        }
    }

    public class TurretModelProxy : EnemyModelProxy
    {
        public TurretModelProxy(EnemyModel data) : base(data)
        {
        }

        public override void Init(IController controller)
        {
            TryAddComponent(new EnemyComponent());
            TryAddComponent(Data.GetComboComponent(controller));
            TryAddComponent(Data.Health.GetComponent());
        }
    }
}


