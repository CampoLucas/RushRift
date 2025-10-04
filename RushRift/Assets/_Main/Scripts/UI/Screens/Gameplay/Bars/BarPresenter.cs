using UnityEngine;

namespace Game.UI.Screens
{
    public class BarPresenter : UIPresenter<BarModel, BarView>
    {
        public override void Begin()
        {
            base.Begin();
            if (Model.OnValueChanged.TryGetValue(out var subject))
            {
                subject.Attach(View);
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning("WARNING: couldn't find the subject in the BarModel, not attaching the view.", this);
#endif
            }
            View.SetStartValue(Model.Data.StartValue, Model.Data.StartMaxValue);
        }

        public override void End()
        {
            base.End();
            if (Model.OnValueChanged.TryGetValue(out var subject))
            {
                subject.Detach(View);
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning("WARNING: couldn't find the subject in the BarModel, not detaching the view.", this);
#endif
            }
        }
        
    }

    
}