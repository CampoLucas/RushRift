namespace Game.UI.Screens
{
    public class BarPresenter : UIPresenter<BarModel, BarView>
    {
        public BarPresenter(BarModel model, BarView view) : base(model, view) { }

        public override void Begin()
        {
            base.Begin();
            Model.OnValueChanged.Attach(View);
            View.SetStartValue(Model.Data.StartValue, Model.Data.StartMaxValue);
        }

        public override void End()
        {
            base.End();
            Model.OnValueChanged.Detach(View);
        }
        
    }
}