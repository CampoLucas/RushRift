using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.UI.Screens
{
    public class SegmentedFillBarView : BarView
    {
        [Header("Prefabs")]
        [SerializeField] private BarSegment barSegmentPrefab;

        [Header("References")]
        [SerializeField] private RectTransform container;
        [SerializeField] private TMP_Text text;

        [Header("Settings")]
        [SerializeField] private int segmentsCount = 10;
        [SerializeField] private float fadeDelay = 0.2f;
        [SerializeField] private float fadeSpeed = 0.5f; 

        [Header("Colors")]
        [SerializeField] private Color filledColor = Color.cyan;
        [SerializeField] private Color secondaryColor = Color.red;
        [SerializeField] private Color emptyColor = Color.grey;

        private List<BarSegment> _segments = new();
        private Coroutine _secondaryCoroutine;
        
        private float _valuePerSegment;
        private float _lastMax = -1;

        public override void OnNotify(float currentHealth, float previousHealth, float maxHealth)
        {
            SetValue(currentHealth, previousHealth, maxHealth);
        }

        public override void SetStartValue(float startValue, float startMaxValue)
        {
            if (!Mathf.Approximately(startMaxValue, _lastMax))
            {
                RebuildSegments(startMaxValue);
            }
            
            text.text = $"{startValue}/{startMaxValue}";

            var remaining = startValue;

            for (var i = 0; i < _segments.Count; i++)
            {
                var segmentFill = Mathf.Clamp01(remaining / _valuePerSegment);
                _segments[i].Fill(segmentFill);
                remaining -= _valuePerSegment;
            }
        }
        
        private void SetValue(float current, float previous, float max)
        {
            if (!Mathf.Approximately(max, _lastMax))
            {
                RebuildSegments(max);
            }

            text.text = $"{current}/{max}";

            var remaining = current;

            for (var i = 0; i < _segments.Count; i++)
            {
                var segmentFill = Mathf.Clamp01(remaining / _valuePerSegment);
                _segments[i].Fill(segmentFill);  // Only adjust fill amount
                remaining -= _valuePerSegment;
            }

            if (current < previous)
            {
                if (_secondaryCoroutine != null) StopCoroutine(_secondaryCoroutine);
                
                _secondaryCoroutine = StartCoroutine(FadeSegmentsCoroutine(current, previous, max));
            }
        }
        
        private void RebuildSegments(float max)
        {
            _lastMax = max; // Store for next comparison


            var newSegmentsCount = segmentsCount; // Calculate new segment count
            _valuePerSegment = max / newSegmentsCount;

            var currentCount = _segments.Count;

            if (newSegmentsCount > currentCount)
            {
                var toAdd = newSegmentsCount - currentCount; // Add new segments.

                for (var i = 0; i < toAdd; i++)
                {
                    var newSegment = Instantiate(barSegmentPrefab, container);
                    newSegment.SetColor(filledColor);
                    newSegment.SetSecondaryColor(secondaryColor);
                    _segments.Add(newSegment);
                }
            }
            else if (newSegmentsCount < currentCount)
            {
                var toRemove = currentCount - newSegmentsCount;

                for (var i = 0; i < toRemove; i++)
                {
                    var index = _segments.Count - 1;
                    Destroy(_segments[index].gameObject);
                    _segments.RemoveAt(index);
                }
            }
        }
        
        private IEnumerator FadeSegmentsCoroutine(float current, float previous, float max)
        {
            yield return new WaitForSeconds(fadeDelay);

            var target = current;
            var start = previous;
            var remaining = start;

            while (remaining >= target)
            {
                remaining -= Time.deltaTime * fadeSpeed;

                var r = remaining;
                for (var i = 0; i < _segments.Count; i++)
                {
                    var segmentFill = Mathf.Clamp01(r / _valuePerSegment);
                    _segments[i].SecondaryFill(segmentFill);  // Only adjust fill amount
                    r -= _valuePerSegment;
                }

                yield return null;
            }
            
            for (var i = 0; i < _segments.Count; i++)
            {
                _segments[i].SecondaryFill(Mathf.Clamp01(target / _valuePerSegment));
                target -= _valuePerSegment;
            }
        }
        

        private void OnDestroy()
        {
            _segments.Clear();
            _segments = null;
        }
    }
}