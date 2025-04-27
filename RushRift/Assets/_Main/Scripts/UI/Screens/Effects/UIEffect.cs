using UnityEngine;

namespace Game.UI.Screens
{
    public class UIEffect : ScriptableObject
    {
        public void Do(UIState from, UIState to, float t, float duration)
        {
            from.FadeOut(t, 0, duration);
            to.FadeOut(t, 0, duration);
        }
    }
}