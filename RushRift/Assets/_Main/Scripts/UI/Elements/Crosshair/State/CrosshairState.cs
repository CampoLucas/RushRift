using System.Linq;
using Game.Entities;
using UnityEngine;

namespace Game.UI.Elements.Crosshair
{
    [CreateAssetMenu(menuName = "Game/UI/Crosshair")]
    public class CrosshairState : ScriptableObject
    {
        public CrosshairView ViewPrefab => view;
        
        [Header("Visuals")]
        [SerializeField] private CrosshairView view;

        [Header("Triggers")]
        [SerializeField] private SerializableSOCollection<CrosshairTrigger> startTriggers;
        [SerializeField] private SerializableSOCollection<CrosshairTrigger> stopTriggers;

        public CrosshairStateInstance CreateInstance(IController controller)
        {
            var start = startTriggers.Select(t => t.GetTrigger(controller)).ToArray();
            var stop = stopTriggers.Select(t => t.GetTrigger(controller)).ToArray();

            return new CrosshairStateInstance(this, start, stop);
        }
    }
}