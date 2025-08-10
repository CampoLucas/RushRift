using Game.Entities.Components;
using UnityEngine;

namespace Game.Entities
{
    public class LaserModel : EntityModel<LaserModelSO>
    {
        
        public LaserModel(LaserModelSO data) : base(data)
        {
        }

        public override void Init(IController controller)
        {
            base.Init(controller);

            TryAddComponent(Data.GetLaserComponent(controller.Origin));
        }
    }
}