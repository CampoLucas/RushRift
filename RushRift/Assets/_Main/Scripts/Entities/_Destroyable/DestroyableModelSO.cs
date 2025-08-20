using UnityEngine;

namespace Game.Entities
{
    [CreateAssetMenu(menuName = "Game/Entities/Destroyable")]
    public class DestroyableModelSO : EntityModelSO
    {
        public override NullCheck<IModel> GetProxy()
        {
            return new NullCheck<IModel>(new EntityModel<DestroyableModelSO>(this));
        }

        public override void Init(in IController controller, in IModel model)
        {
            if (controller.TryGetObserver(EntityController.DESTROY, out var observer))
            {
                model.TryAddComponent(new DestroyableComponent(observer));
            }
        }
    }
}