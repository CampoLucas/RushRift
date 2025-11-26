using UnityEngine;

namespace Game.UI.Elements
{
    [CreateAssetMenu(menuName = "Game/UI/Upgrade Icon")]
    public class UpgradeIcon : ScriptableObject
    {
        public Sprite IconSprite => icon;
        
        [SerializeField] private Sprite icon;
    }
}