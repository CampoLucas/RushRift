using System;
using System.Collections.Generic;
using Game.UI.Mediator;
using Game.UI.Screens.Interfaces;
using MyTools.Global;
using UnityEngine;

namespace Game.UI.Screens
{
    
    public sealed class LevelSelectorMediator : UIMediator
    {
        protected override void InitActions(in Dictionary<MenuState, Action> actions)
        {
            actions.TryAdd(MenuState.Levels, () => SetState(UIScreen.Levels));
            actions.TryAdd(MenuState.GameModes, () => SetState(UIScreen.GameModes));
        }
    }
}