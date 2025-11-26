using UnityEngine;

namespace Game.UI.Elements
{
    [CreateAssetMenu(menuName = "Game/UI/Upgrade Icon")]
    public class UpgradeIcon : ScriptableObject
    {
        public Texture Icon => icon;
        public Color Color => color;
        public Material UnlockedMat => unlockedMat;
        public Material LockedMat => lockedMat;
        
        [SerializeField] private Texture icon;
        [SerializeField] private Color color;
        [SerializeField] private Material unlockedMat;
        [SerializeField] private Material lockedMat;
    }
}