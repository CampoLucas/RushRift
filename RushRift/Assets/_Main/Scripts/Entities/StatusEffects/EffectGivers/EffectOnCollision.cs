using System;
using Game.Entities;
using UnityEngine;

public class EffectOnCollision : MonoBehaviour
{
    public Action OnApplied = delegate { };
    
    [Header("Effect")]
    [SerializeField] private EffectGiver effectGiver;

    [Header("Settings")]
    [SerializeField] private bool destroyWhenApplied = true;
    [SerializeField] private bool onTrigger = true;
    [SerializeField] private bool onCollision;
    [SerializeField] private bool onStay;

    private bool _applied;

    public void ResetEffect()
    {
        _applied = false;
    }
    
    private void GiveEffect(GameObject other)
    {
        if (_applied) return;
        
        if (other.TryGetComponent<IController>(out var controller))
        {
            effectGiver.ApplyEffect(controller);

            _applied = true;
            OnApplied.Invoke();
            if (destroyWhenApplied) Destroy(gameObject);
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (!onTrigger || !effectGiver.HasEffect()) return;
        GiveEffect(other.gameObject);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!onStay) return;
        if (!onTrigger || !effectGiver.HasEffect()) return;
        GiveEffect(other.gameObject);
    }

    private void OnCollisionEnter(Collision other)
    {
        if (!onCollision || !effectGiver.HasEffect()) return;
        GiveEffect(other.gameObject);
    }

    private void OnCollisionStay(Collision other)
    {
        if (!onStay) return;
        if (!onCollision || !effectGiver.HasEffect()) return;
        GiveEffect(other.gameObject);
    }

    private void OnDestroy()
    {
        effectGiver.Dispose();
        effectGiver = null;
        OnApplied = null;
    }
}
