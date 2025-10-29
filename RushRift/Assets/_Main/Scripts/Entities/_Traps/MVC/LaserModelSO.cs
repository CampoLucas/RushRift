using Game.Entities.Components;
using UnityEngine;

namespace Game.Entities
{
    [CreateAssetMenu(menuName = "Game/Entities/Traps/Laser/Model")]
    public class LaserModelSO : EntityModelSO
    {
        [SerializeField] private HealthComponentData _health;
        [SerializeField] private LaserComponentData _laser;
        
        public override NullCheck<IModel> GetProxy()
        {
            return new NullCheck<IModel>(new EntityModel<LaserModelSO>(this));
        }

        public LaserComponent GetLaserComponent(Transform origin)
        {
            return new LaserComponent(origin, _laser);
        }
        
        public override void Init(in IController controller, in IModel model)
        {
            var c = controller;
            model.TryAddComponent(HealthComponentFactory);
            model.TryAddComponent(() => GetLaserComponent(c.Origin));
        }
        
        private HealthComponent HealthComponentFactory() => _health.GetComponent();
    }
}