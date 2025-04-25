using System;
using Game.Entities.Components;

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
    }
}