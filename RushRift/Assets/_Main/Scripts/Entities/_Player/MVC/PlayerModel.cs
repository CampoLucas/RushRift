using UnityEngine;

namespace Game.Entities
{
    public class PlayerModel : EntityModel<PlayerModelSO>
    {
        public PlayerModel(PlayerModelSO model) : base(model)
        {
            
        }

        public override void Init(IController controller)
        {
            base.Init(controller);

            var playerObject = controller.Origin.gameObject;
            if (playerObject.TryGetComponent<Rigidbody>(out var rigidBody) && playerObject.TryGetComponent<CapsuleCollider>(out var collider))
            {
                var movement = Data.GetMotionController(rigidBody, collider, controller.Origin, controller.Joints.GetJoint(EntityJoint.Eyes));
                TryAddComponent(movement);
            }

            TryAddComponent(Data.GetComboComponent(controller));
            TryAddComponent(Data.Health.GetComponent()); 
            TryAddComponent(Data.Energy.GetComponent());
            
        }
    }
}