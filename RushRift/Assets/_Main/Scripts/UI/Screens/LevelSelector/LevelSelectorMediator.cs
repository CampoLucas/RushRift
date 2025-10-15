using System;
using System.Collections.Generic;
using Game.DesignPatterns.Observers;
using Game.General;
using Game.Levels;
using Game.UI.Mediator;
using Game.UI.Screens.Elements;
using Game.UI.Screens.Interfaces;
using MyTools.Global;
using UnityEngine;

namespace Game.UI.Screens
{
    
    public sealed class LevelSelectorMediator : UIMediator
    {
        public static ISubject<GameModeSO> GameModeSelected { get; private set; } = new Subject<GameModeSO>();
        public static ISubject<BaseLevelSO> LevelSelected { get; private set; } = new Subject<BaseLevelSO>();

        

        protected override void InitActions(ref Dictionary<MenuState, Action> actions)
        {
            actions.TryAdd(MenuState.Levels, () => SetState(UIScreen.Levels));
            actions.TryAdd(MenuState.GameModes, () => SetState(UIScreen.GameModes));
        }

        public override void Dispose()
        {
            base.Dispose();
            
            GameModeSelected.DetachAll();
            LevelSelected.DetachAll();
        }
    }
}