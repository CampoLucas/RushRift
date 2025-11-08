using System.Collections.Generic;
using Game.Entities;
using UnityEngine;

namespace Game.Levels
{
    [CreateAssetMenu(menuName = "Game/Levels/Tutorial Level", fileName = "New Tutorial Level Config")]
    public class TutorialLevelSO : LevelSO
    {
        [Header("Tutorial Effects")]
        [SerializeField] private List<Effect> effectsToAdd;
        
        public override int TryGetEffects(out Effect[] effect)
        {
            effect = effectsToAdd.ToArray();
            
            return effect.Length;
        }
    }
}