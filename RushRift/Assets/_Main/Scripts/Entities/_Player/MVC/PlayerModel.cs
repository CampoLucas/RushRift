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

            if (controller.Transform.gameObject.TryGetComponent<CharacterController>(out var characterController))
            {
                TryAddComponent(Data.MoveSpeed.GetMovement(characterController));
            }
            
            TryAddComponent(Data.Health.GetComponent()); 
            TryAddComponent(Data.Stamina.GetComponent());
            TryAddComponent(Data.Mana.GetComponent());
        }
    }
}