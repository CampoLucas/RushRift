using Game.Entities.Components;
using UnityEngine;

namespace Game.Entities
{
    [CreateAssetMenu(menuName = "Game/Entities/Traps/Laser/Model")]
    public class LaserModelSO : EntityModelSO
    {
        [SerializeField] private LaserComponentData _laser;
        
        public override NullCheck<IModel> GetProxy()
        {
            return new NullCheck<IModel>(new LaserModel(this));
        }

        public LaserComponent GetLaserComponent(Transform origin)
        {
            return new LaserComponent(origin, _laser);
        }
    }
}