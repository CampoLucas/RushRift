using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game.UI.StateMachine
{
    public class SegmentedBarView : BarView
    {
        [FormerlySerializedAs("barPrefab")]
        [Header("Prefabs")]
        [SerializeField] private BarSegment barSegmentPrefab;

        [Header("References")]
        [SerializeField] private RectTransform container;
        [SerializeField] protected TMP_Text text;
        [SerializeField] protected Image icon;

        [Header("Settings")]
        //[SerializeField] private int segmentsCount = 10;
        [SerializeField] protected float fadeDelay = 0.2f;
        [SerializeField] private float fadeSpeed = 0.5f; 

        [Header("Colors")]
        [SerializeField] protected Color filledColor = Color.cyan;
        [SerializeField] protected Color secondaryColor = Color.red;
        [SerializeField] protected Color emptyColor = Color.grey;

        protected List<BarSegment> Segments = new();
        private Coroutine _secondaryCoroutine;
        
        private float _valuePerSegment;
        private float _lastMax = -1;

        public override void OnNotify(float currentHealth, float previousHealth, float maxHealth)
        {
            SetValue(currentHealth, previousHealth, maxHealth);
        }

        public override void SetStartValue(float current, float max)
        {
            text.text = ValueText(current, max);
            
            if (!Mathf.Approximately(max, _lastMax)) // Check if we need to regenerate segments
            {
                RebuildSegments(max);
            }

            var filledSegments = Mathf.FloorToInt(current / _valuePerSegment);

            for (var i = 0; i < Segments.Count; i++)
            {
                Segments[i].SetColor(i < filledSegments ? filledColor : emptyColor);
            }
        }

        private string ValueText(float current, float max)
        {
            //return $"<size=100%>{((int)current)}<voffset=.25em><size=50%>/{(int)max}";
            return ((int)current).ToString();
        }
        
        private void SetValue(float current, float previous, float max)
        {
            text.text = ValueText(current, max);
            
            if (!Mathf.Approximately(max, _lastMax)) // Check if we need to regenerate segments
            {
                RebuildSegments(max);
            }

            var previousFilled = Mathf.FloorToInt(previous / _valuePerSegment);
            var currentFilled = Mathf.FloorToInt(current / _valuePerSegment);
            
            
            
            for (var i = 0; i < Segments.Count; i++) // Instantly update filled segments
            {
                if (i < currentFilled)
                {
                    Segments[i].SetColor(filledColor);
                }
                else if (i >= currentFilled && i < previousFilled)
                {
                    // if Just lost fill, switch to secondary color
                    Segments[i].SetColor(secondaryColor);
                }
                else
                {
                    Segments[i].SetColor(emptyColor); // Already empty
                }
            }

            // Stop any running coroutines
            if (_secondaryCoroutine != null)
                StopCoroutine(_secondaryCoroutine);
                    
            if (current < previous)
            {
                _secondaryCoroutine = StartCoroutine(FadeSegmentsCoroutine(current, previous, max));
            }
        }
        
        private void RebuildSegments(float max)
        {
            _lastMax = max; // Store for next comparison


            var newSegmentsCount = max; // Calculate new segment count
            _valuePerSegment = 1;

            var currentCount = Segments.Count;

            if (newSegmentsCount > currentCount)
            {
                var toAdd = newSegmentsCount - currentCount; // Add new segments.

                for (var i = 0; i < toAdd; i++)
                {
                    var newSegment = Instantiate(barSegmentPrefab, container);
                    newSegment.SetColor(emptyColor);
                    Segments.Add(newSegment);
                }
            }
            else if (newSegmentsCount < currentCount)
            {
                var toRemove = currentCount - newSegmentsCount;

                for (var i = 0; i < toRemove; i++)
                {
                    var index = Segments.Count - 1;
                    Destroy(Segments[index].gameObject);
                    Segments.RemoveAt(index);
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
                
                for (var i = 0; i < Segments.Count; i++)
                {
                    if (i > indexThreshold)
                    {
                        Segments[i].SetColor(emptyColor);
                    }
                }

                yield return null;
            }

            for (var i = 0; i < Segments.Count; i++)
            {
                if (i > targetFill) Segments[i].SetColor(emptyColor);
            }

        }

        protected virtual void OnDestroy()
        {
            if (_secondaryCoroutine != null) StopCoroutine(_secondaryCoroutine);
            
            Segments.Clear();
            Segments = null;
        }
    }
}