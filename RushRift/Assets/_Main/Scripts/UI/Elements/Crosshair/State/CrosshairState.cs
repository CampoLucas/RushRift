using System.Linq;
using Game.Entities;
using UnityEngine;

namespace Game.UI.Elements.Crosshair
{
    [CreateAssetMenu(menuName = "Game/UI/Crosshair")]
    public class CrosshairState : ScriptableObject
    {
        public Sprite Sprite => sprite;
        public float Alpha => Mathf.Clamp01(alpha);
        
        [Header("Visuals")]
        [SerializeField] private Sprite sprite;
        [SerializeField, Range(0, 1)] private float alpha;

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