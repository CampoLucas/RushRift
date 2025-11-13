using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game.UI.StateMachine
{
    public class FillBarView : BarView
    {
        [Header("References")]
        [SerializeField] private Image primaryFill;
        [SerializeField] private Image secondaryFill;
        [SerializeField] private Image background;
        [SerializeField] private Image border;
        [SerializeField] private TMP_Text text;

        [Header("Settings")]
        [SerializeField] private bool useSecondaryFill;
        [SerializeField] private bool showText;
        [SerializeField] private bool showMax;
        [SerializeField] private float secondarySpeed = 1f;
        
        [Header("Colors")]
        [SerializeField] private Color primaryFillColor;
        [SerializeField] private Color secondaryFillColor;
        [SerializeField] private Color backgroundColor;
        [SerializeField] private Color borderColor;
        [SerializeField] private Color textColor;

        private Coroutine _secondaryCoroutine; 

        private void Start()
        {
            if (primaryFill)
            {
                primaryFill.color = primaryFillColor;
            }

            if (background)
            {
                background.color = backgroundColor;
            }

            if (border)
            {
                border.color = borderColor;
            }
            
            if (secondaryFill)
            {
                secondaryFill.gameObject.SetActive(false);
                secondaryFill.color = secondaryFillColor;
            }
            
            if (text)
            {
                if (!showText)
                {
                    text.gameObject.SetActive(false);
                }
                else
                {
                    text.color = textColor;
                }
            }
        }

        public override void OnNotify(float currentHealth, float previousHealth, float maxHealth)
        {
            SetValue(currentHealth, previousHealth, maxHealth);
        }

        public override void SetStartValue(float current, float max)
        {
            primaryFill.fillAmount = current / max;
            if (showText) text.text = showMax ? $"{(int)current}/{max}" : $"{(int)current}";
        }

        private void SetValue(float current, float previous, float max)
        {
            primaryFill.fillAmount = current / max;
            
            if (showText) text.text = showMax ? $"{(int)current}/{max}" : $"{(int)current}";
            
            
            if (useSecondaryFill && current < previous)
            {
                if (_secondaryCoroutine != null)
                {
                    StopCoroutine(_secondaryCoroutine);
                }
                _secondaryCoroutine = StartCoroutine(SetSecondaryValue(current, previous, max));
            }
        }

        private IEnumerator SetSecondaryValue(float current, float previous, float max)
        {
            var targetFill = current / max;
            var startFill = previous / max;
            var currentFill = startFill;
            
            secondaryFill.fillAmount = startFill;
            secondaryFill.gameObject.SetActive(true);

            while (currentFill >= targetFill)
            {
                currentFill -= Time.deltaTime * secondarySpeed;

                secondaryFill.fillAmount = currentFill;
                yield return null;
            }

            secondaryFill.fillAmount = targetFill;
            secondaryFill.gameObject.SetActive(false);
        }
    }
}