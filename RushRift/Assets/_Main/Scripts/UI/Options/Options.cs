using System;
using Game.DesignPatterns.Observers;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class Options : MonoBehaviour
    {
        public static ISubject<float> OnCameraSensibilityChanged = new Subject<float>();
        public static ISubject<float> OnCameraSmoothnessChanged = new Subject<float>();

        [Header("Camera Settings")]
        [SerializeField] private OptionSlider sensibilitySlider;
        [SerializeField] private OptionSlider smoothnessSlider;

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
            
            Debug.Log("Options Awake");
            
            sensibilitySlider.OnValueChanged.AddListener(OnSensibilityChangedHandler);
            smoothnessSlider.OnValueChanged.AddListener(OnSmoothnessChangedHandler);
        }

        private void Start()
        {
            // init values from the save file
            var saveData = SaveAndLoad.Load();

            sensibilitySlider.Value = saveData.camera.Sensibility;
            smoothnessSlider.Value = saveData.camera.Smoothness;
        }

        public void OnSensibilityChangedHandler(float value)
        {
            OnCameraSensibilityChanged.NotifyAll(value);
            
            // save value
            var saveData = SaveAndLoad.Load();

            saveData.camera.Sensibility = value;
            SaveAndLoad.Save(saveData);
        }
        
        public void OnSmoothnessChangedHandler(float value)
        {
            OnCameraSmoothnessChanged.NotifyAll(value);
            
            // Save value
            var saveData = SaveAndLoad.Load();

            saveData.camera.Smoothness = value;
            SaveAndLoad.Save(saveData);
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
            else
            {
                return;
            }
            
            OnCameraSmoothnessChanged.DetachAll();
            //OnCameraSmoothnessChanged.Dispose();
            
            OnCameraSensibilityChanged.DetachAll();
            //OnCameraSensibilityChanged.Dispose();
        }
    }
}