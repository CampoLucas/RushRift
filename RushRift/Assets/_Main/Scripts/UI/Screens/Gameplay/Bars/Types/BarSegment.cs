using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Screens
{
    public class BarSegment : MonoBehaviour
    {
        [SerializeField] private Image segment;
        
        public void SetColor(Color color)
        {
            segment.color = color;
        }
    }
}