using Game.Entities;
using UnityEngine;

public class EffectOnCollision : MonoBehaviour
{
    [SerializeField] private EffectGiver effectGiver;
    [SerializeField] private bool onTrigger = true;
    [SerializeField] private bool onCollision;

    private void GiveEffect(GameObject other)
    {
        if (other.TryGetComponent<IController>(out var controller))
        {
            effectGiver.ApplyEffect(controller);
            
            Destroy(gameObject);
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (!onTrigger || !effectGiver.HasEffect()) return;
        GiveEffect(other.gameObject);
    }

    private void OnCollisionEnter(Collision other)
    {
        if (!onCollision || !effectGiver.HasEffect()) return;
        GiveEffect(other.gameObject);
    }
}
