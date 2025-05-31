using System;
using Game.DesignPatterns.Observers;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class Options : MonoBehaviour
    {
        public static ISubject<float> OnCameraSensibilityChanged;
        public static ISubject<float> OnCameraSmoothnessChanged;

        [Header("Camera Settings")]
        [SerializeField] private Slider sensibilitySlider;
        [SerializeField] private Slider smoothnessSlider;

        private Options _instance;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else
            {
                Destroy(this);
                return;
            }
            
            OnCameraSensibilityChanged = new Subject<float>();
            OnCameraSmoothnessChanged = new Subject<float>();
            
            sensibilitySlider.onValueChanged.AddListener(OnSensibilityChangedHandler);
            smoothnessSlider.onValueChanged.AddListener(OnSmoothnessChangedHandler);
        }

        private void Start()
        {
            // init values from the save file
            var saveData = SaveAndLoad.Load();

            sensibilitySlider.value = saveData.CameraSettings.Sensibility;
            smoothnessSlider.value = saveData.CameraSettings.Smoothness;
        }

        public void OnSensibilityChangedHandler(float value)
        {
            OnCameraSensibilityChanged.NotifyAll(value);
            
            // save value
            var saveData = SaveAndLoad.Load();

            saveData.CameraSettings.Sensibility = value;
            SaveAndLoad.Save(saveData);
        }
        
        public void OnSmoothnessChangedHandler(float value)
        {
            OnCameraSmoothnessChanged.NotifyAll(value);
            
            // Save value
            var saveData = SaveAndLoad.Load();

            saveData.CameraSettings.Smoothness = value;
            SaveAndLoad.Save(saveData);
        }
    }
}