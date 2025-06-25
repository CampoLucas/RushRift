using TMPro;
using UnityEngine;

namespace Game.UI.Screens
{
    public class PauseView : UIView
    {
        [SerializeField] private TMP_Text versionText;

        protected override void Awake()
        {
            base.Awake();

            versionText.text = $"Version: {Application.version}";
        }
    }
}