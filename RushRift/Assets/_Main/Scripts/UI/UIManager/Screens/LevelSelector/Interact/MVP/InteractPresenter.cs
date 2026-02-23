using System;
using System.Collections.Generic;
using Game.DesignPatterns.Observers;
using Game.General;
using Game.Levels;
using Game.Saves;
using Game.UI.StateMachine.Elements;
using MyTools.Global;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.StateMachine
{
    public sealed class InteractPresenter : UIPresenter<InteractModel, InteractView>
    {
        public override bool TryGetState(out UIState state)
        {
            state = new InteractState(this);
            return true;
        }
    }
}