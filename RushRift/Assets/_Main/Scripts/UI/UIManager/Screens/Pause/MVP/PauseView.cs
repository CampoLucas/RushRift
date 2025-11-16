using TMPro;
using UnityEngine;

namespace Game.UI.StateMachine
{
    public class PauseView : UIView
    {
        [SerializeField] private TMP_Text versionText;
        [SerializeField] private TMP_Text userText;

        protected override void Awake()
        {
            base.Awake();

            versionText.text = $"Version: {Application.version}";
        }

        public override void Dispose()
        {
            versionText = null;
            base.Dispose();
        }
    }
}