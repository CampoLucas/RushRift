using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Elements
{
    [DisallowMultipleComponent]
    public class UpgradeSlot : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Color enabledColor = Color.white;
        [SerializeField] private Color disabledColor = Color.gray;
        
        [Header("Visuals")]
        [SerializeField] private Image iconImg;
        
        private UpgradeIcon _icon;

        public void Init(UpgradeIcon icon, bool state)
        {
            _icon = icon;

            iconImg.sprite = _icon.IconSprite;
            iconImg.color = state ? enabledColor : disabledColor;
        }
    }
}