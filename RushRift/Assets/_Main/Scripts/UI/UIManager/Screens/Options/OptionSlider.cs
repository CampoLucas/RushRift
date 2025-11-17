using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game.UI
{
    public class OptionSlider : MonoBehaviour
    {
        public Slider.SliderEvent OnValueChanged => slider.onValueChanged;

        public float Value
        {
            get => slider.value;
            set => slider.value = value;
        }
        
        [SerializeField] private Slider slider;
        [SerializeField] private TMP_Text valueText;

        private void Awake()
        {
            slider.onValueChanged.AddListener(OnValueChangedHandler);
        }

        private void OnValueChangedHandler(float value)
        {
            valueText.text = value.ToString("0.00");
        }

        private void OnDestroy()
        {
            slider.onValueChanged.RemoveListener(OnValueChangedHandler);
            slider = null;
            valueText = null;
        }
    }
}