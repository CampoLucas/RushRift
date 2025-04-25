using UnityEngine;
using UnityEngine.UI;

namespace Game.Entities.AttackSystem
{
    [CreateAssetMenu(menuName = "Game/Effects/Effect")]
    public class EffectData : ScriptableObject
    {
        public EffectUI UI => ui;
        public EffectStats Stats => stats;
        
        [Header("UI")]
        [SerializeField] private EffectUI ui;

        [Header("Stats")]
        [SerializeField] private EffectStats stats;

    }

    [System.Serializable]
    public class EffectUI
    {
        public string Name => name;
        public string Description => description;
        public Sprite Icon => icon;
        
        [Header("General")]
        [SerializeField] private string name;
        [SerializeField][TextArea] private string description;
        
        [Header("Visuals")]
        [SerializeField] private Sprite icon;
    }

    [System.Serializable]
    public class EffectStats
    {
        [Header("Multishot")]
        [SerializeField] private bool multishot;
        [SerializeField] private int multishotAmount;

        [Header("Forward")]
        [SerializeField] private bool forward;
        [SerializeField] private int forwardAmount;

        [Header("Diagonal")]
        [SerializeField] private bool diagonal;
        [SerializeField] private int diagonalAmount;

        [Header("Penetration")]
        [SerializeField] private bool penetration;
        [SerializeField] private int penetrationAmount;

        [Header("Wall Bounce")]
        [SerializeField] private bool wallBounce;
        [SerializeField] private int wallBounceAmount;

        [Header("Enemy Bounce")]
        [SerializeField] private bool enemyBounce;
        [SerializeField] private int enemyBounceAmount;

        [Header("Gravity")]
        [SerializeField] private bool gravity;

        [Header("Size")]
        [SerializeField] private bool size;
        [SerializeField] private float sizePercentage;

        public bool ApplyEffects(IController controller)
        {
            if (!controller.GetModel().TryGetComponent<ComboHandler>(out var handler)) return false;
            
            if (multishot)
            {
                handler.ComboStats.IncreaseMultiShot(multishotAmount);
            }
            if (forward)
            {
                handler.ComboStats.IncreaseForwardAmount(forwardAmount);
            }
            if (diagonal)
            {
                handler.ComboStats.IncreaseDiagonalAmount(diagonalAmount);
            }
            if (penetration)
            {
                handler.ComboStats.IncreasePenetration(penetrationAmount);
            }
            if (wallBounce)
            {
                handler.ComboStats.IncreaseWallBounce(wallBounceAmount);
            }
            if (enemyBounce)
            {
                handler.ComboStats.IncreaseEnemyBounce(enemyBounceAmount);
            }
            if (gravity)
            {
                handler.ComboStats.SetHasGravity(gravity);
            }
            if (size)
            {
                handler.ComboStats.IncreaseSize(sizePercentage);
            }

            return true;
        }
    }
}