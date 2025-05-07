using UnityEngine;

namespace Game.Entities.AttackSystem
{
    public class ModuleGiver : MonoBehaviour
    {
        [SerializeField] private StaticModuleData data;
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.TryGetComponent<IController>(out var controller))
            {

                if (controller.GetModel().TryGetComponent<ComboHandler>(out var component))
                {
                    component.AddModule(data.Test());
                    gameObject.SetActive(false);
                }
            }
        }
    }
}