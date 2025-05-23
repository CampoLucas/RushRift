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

            if (controller.Origin.gameObject.TryGetComponent<CharacterController>(out var characterController))
            {
                //var movement = Data.MoveSpeed.GetMovement(characterController);
                var movement = Data.PlayerMovement.GetMovement(characterController, controller.Origin, controller.Joints.GetJoint(EntityJoint.Eyes));
                TryAddComponent(movement);

                TryAddComponent(Data.Dash.GetComponent(characterController, controller.Origin, Camera.main.transform));
                //TryAddComponent(Data.SpeedLines.GetComponent(controller.SpeedLines, movement.MoveAmount));
            }

            TryAddComponent(Data.GetComboComponent(controller));
            TryAddComponent(Data.Health.GetComponent()); 
            TryAddComponent(Data.Energy.GetComponent());
            
        }
    }
}