using Game.Entities;
using Game.Entities.Components;

namespace Game.UI.Screens
{
    public sealed class GameplayState : UIState<GameplayPresenter, GameplayModel, GameplayView>
    {
        public GameplayState(IModel playerModel, GameplayPresenter presenter) : base(presenter)
        {
            if (playerModel == null || !playerModel.TryGetComponent<HealthComponent>(out var health)) return;
            if (!playerModel.TryGetComponent<EnergyComponent>(out var energy)) return;

            var healthBarData = new AttributeBarData(health, health.OnValueChanged);
            var energyBarData = new AttributeBarData(energy, energy.OnValueChanged);
            
            var gameplayModel = new GameplayModel(healthBarData, energyBarData);
            presenter.Init(gameplayModel);
        }
    }
}