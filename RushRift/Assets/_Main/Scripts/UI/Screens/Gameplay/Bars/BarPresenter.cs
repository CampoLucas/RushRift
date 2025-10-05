using MyTools.Global;
using UnityEngine;
using Logger = MyTools.Global.Logger;

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
                this.Log("Couldn't find the subject in the BarModel, not attaching the view", LogType.Warning);
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
                this.Log("Couldn't find the subject in the BarModel, not detaching the view", LogType.Warning);
            }
        }
        
    }

    
}