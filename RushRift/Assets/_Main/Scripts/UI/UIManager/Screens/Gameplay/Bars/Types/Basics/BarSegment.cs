using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.StateMachine
{
    public class BarSegment : MonoBehaviour
    {
        [SerializeField] private Image segment;
        [SerializeField] private Image secondarySegment;
        
        public void SetColor(Color color)
        {
            segment.color = color;
        }
        
        public void SetSecondaryColor(Color color)
        {
            if (secondarySegment) secondarySegment.color = color;
        }

        public void Fill(float amount)
        {
            segment.fillAmount = amount;
        }
        
        public void SecondaryFill(float amount)
        {
            if (secondarySegment) secondarySegment.fillAmount = amount;
        }
    }
}