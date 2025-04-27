namespace Game.UI.Screens
{
    public class AttributeBarPresenter : UIPresenter<AttributeBarModel, AttributeBarView>
    {
        public AttributeBarPresenter(AttributeBarModel model, AttributeBarView view) : base(model, view) { }

        public override void Begin()
        {
            base.Begin();
            Model.OnValueChanged.Attach(View);
            View.SetValue(Model.Data.StartValue, Model.Data.StartMaxValue);
        }

        public override void End()
        {
            base.End();
            Model.OnValueChanged.Detach(View);
        }
        
    }
}