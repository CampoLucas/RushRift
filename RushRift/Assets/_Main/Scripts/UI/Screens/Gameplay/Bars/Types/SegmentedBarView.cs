using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.UI.Screens
{
    public class SegmentedBarView : BarView
    {
        [FormerlySerializedAs("barPrefab")]
        [Header("Prefabs")]
        [SerializeField] private BarSegment barSegmentPrefab;

        [Header("References")]
        [SerializeField] private RectTransform container;

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
            if (!Mathf.Approximately(startMaxValue, _lastMax)) // Check if we need to regenerate segments
            {
                RebuildSegments(startMaxValue);
            }

            var filledSegments = Mathf.FloorToInt(startValue / _valuePerSegment);

            for (var i = 0; i < _segments.Count; i++)
            {
                _segments[i].SetColor(i < filledSegments ? filledColor : emptyColor);
            }
        }
        
        private void SetValue(float current, float previous, float max)
        {
            if (!Mathf.Approximately(max, _lastMax)) // Check if we need to regenerate segments
            {
                RebuildSegments(max);
            }

            var previousFilled = Mathf.FloorToInt(previous / _valuePerSegment);
            var currentFilled = Mathf.FloorToInt(current / _valuePerSegment);
            
            
            
            for (var i = 0; i < _segments.Count; i++) // Instantly update filled segments
            {
                if (i < currentFilled)
                {
                    _segments[i].SetColor(filledColor);
                }
                else if (i >= currentFilled && i < previousFilled)
                {
                    // if Just lost fill, switch to secondary color
                    _segments[i].SetColor(secondaryColor);
                }
                else
                {
                    _segments[i].SetColor(emptyColor); // Already empty
                }
            }

            if (current < previous)
            {
                // Stop any running coroutines
                if (_secondaryCoroutine != null)
                    StopCoroutine(_secondaryCoroutine);
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
                    newSegment.SetColor(emptyColor);
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

            var targetFill = Mathf.FloorToInt(current / _valuePerSegment);
            var startFill = Mathf.FloorToInt(previous / _valuePerSegment);
            var currentFill = (float)startFill;

            while (currentFill > targetFill)
            {
                currentFill -= Time.deltaTime * fadeSpeed;

                var indexThreshold = Mathf.FloorToInt(currentFill);
                
                for (var i = 0; i < _segments.Count; i++)
                {
                    if (i > indexThreshold)
                    {
                        _segments[i].SetColor(emptyColor);
                    }
                }

                yield return null;
            }

            for (var i = 0; i < _segments.Count; i++)
            {
                if (i > targetFill) _segments[i].SetColor(emptyColor);
            }

        }

        private void OnDestroy()
        {
            _segments.Clear();
            _segments = null;
        }
    }
}