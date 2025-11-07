using System;
using Game.Entities.Components;
using UnityEngine;

namespace Game.Entities.AttackSystem
{
    public interface IAttack : IDisposable
    {
        float Duration { get; }
        bool Loop { get; }
        void UpdateAttack(ComboHandler comboHandler, float delta);
        void StartAttack(ComboHandler comboHandler);
        void EndAttack(ComboHandler comboHandler);
        bool TryGetTransition(ComboHandler comboHandler, out TransitionProxy transition);
        bool ModulesExecuted();
        void LateUpdateAttack(ComboHandler comboHandler, float delta);
        
        // Gizmos Methods
        void OnDraw(Transform origin);
        void OnDrawSelected(Transform origin);
    }
}