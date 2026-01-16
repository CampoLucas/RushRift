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
        [SerializeField] private RawImage iconImg;
        [SerializeField] private Image backgroundImg;
        
        private UpgradeIcon _icon;

        public void Init(UpgradeIcon icon, bool state)
        {
            _icon = icon;

            iconImg.texture = _icon.Icon;
            iconImg.material = state ? _icon.UnlockedMat : _icon.LockedMat;
            iconImg.color = state ? icon.Color : disabledColor;
            //backgroundImg.color = state ? icon.Color : disabledColor;
        }
    }
}