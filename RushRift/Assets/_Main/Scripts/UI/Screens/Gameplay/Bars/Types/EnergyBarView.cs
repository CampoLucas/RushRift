using System.Collections;
using UnityEngine;

namespace Game.UI.Screens
{
    public class EnergyBarView : SegmentedBarView
    {
        [Header("Charging Anim")]
        [SerializeField] private float blinkSpeed = .5f;
        [SerializeField] private float popScale = 1.3f;
        [SerializeField] private float popDuration = .15f;
        
        private Coroutine _chargingCoroutine;
        private Coroutine _popCoroutine;
        
        public override void OnNotify(float current, float previous, float max)
        {
            base.OnNotify(current, previous, max);

            if (current == 0)
            {
                Charging(true);
            }
            else if (previous == 0 && current > 0)
            {
                Charging(false);
            }
        }

        private void Charging(bool value)
        {
            if (_chargingCoroutine != null) StopCoroutine(_chargingCoroutine);
            if (_popCoroutine != null) StopCoroutine(_popCoroutine);

            if (value)
            {
                _popCoroutine = null;
                text.color = secondaryColor;
                icon.color = secondaryColor;
                
                var segment = Segments[0].transform;
                var textTransform = text.transform;
                var iconTransform = icon.transform;
                
                segment.localScale = Vector3.one;
                textTransform.localScale = Vector3.one;
                iconTransform.localScale = Vector3.one;
                
                _chargingCoroutine = StartCoroutine(ChargingBlink());
            }
            else
            {
                _chargingCoroutine = null;
                Segments[0].SetColor(filledColor);
                text.color = filledColor;
                icon.color = filledColor;

                _popCoroutine = StartCoroutine(FillPopEffect());
            }
        }
        
        private IEnumerator ChargingBlink()
        {
            yield return new WaitForSeconds(fadeDelay);
            
            var segment = Segments[0];
            var toggle = false;

            while (true)
            {
                var color = toggle ? secondaryColor : emptyColor;
                
                segment.SetColor(color);
                text.color = color;
                icon.color = color;
                toggle = !toggle;

                yield return new WaitForSeconds(blinkSpeed);
            }
            
        }
        
        private IEnumerator FillPopEffect()
        {
            var segment = Segments[0].transform;
            var textTransform = text.transform;
            var iconTransform = icon.transform;

            var startScale = Vector3.one;
            var currentScale = segment.localScale;
            var targetScale = startScale * popScale;

            var t = 0f;

            // Scale up
            while (t < popDuration)
            {
                t += Time.deltaTime;
                var progress = t / popDuration;
                var scale = Vector3.Lerp(currentScale, targetScale, progress);

                segment.localScale = scale;
                textTransform.localScale = scale;
                iconTransform.localScale = scale;

                yield return null;
            }

            // Scale down
            t = 0f;
            while (t < popDuration)
            {
                t += Time.deltaTime;
                var progress = t / popDuration;
                var scale = Vector3.Lerp(targetScale, startScale, progress);

                segment.localScale = scale;
                textTransform.localScale = scale;
                iconTransform.localScale = scale;

                yield return null;
            }

            // Reset
            segment.localScale = startScale;
            textTransform.localScale = startScale;
            iconTransform.localScale = startScale;
        }
    }
}