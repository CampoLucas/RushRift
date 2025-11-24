using Game.Entities;
using UnityEngine;

namespace Game.UI.Elements.Crosshair
{
    public abstract class CrosshairTrigger : ScriptableObject
    {
        public abstract Trigger GetTrigger(IController controller);
    }
}