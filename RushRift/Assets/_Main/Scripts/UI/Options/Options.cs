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
        public static ISubject<float> OnMasterVolumeChanged = new Subject<float>();
        public static ISubject<float> OnMusicVolumeChanged = new Subject<float>();
        public static ISubject<float> OnSFXVolumeChanged = new Subject<float>();

        [Header("Camera Settings")]
        [SerializeField] private OptionSlider sensibilitySlider;
        [SerializeField] private OptionSlider smoothnessSlider;

        [Header("Sound Settings")]
        [SerializeField] private OptionSlider masterSlider;
        [SerializeField] private OptionSlider musicSlider;
        [SerializeField] private OptionSlider sfxSlider;

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
            
            sensibilitySlider.OnValueChanged.AddListener(OnSensibilityChangedHandler);
            smoothnessSlider.OnValueChanged.AddListener(OnSmoothnessChangedHandler);
            
            masterSlider.OnValueChanged.AddListener(OnMasterChangedHandler);
            musicSlider.OnValueChanged.AddListener(OnMusicChangedHandler);
            sfxSlider.OnValueChanged.AddListener(OnSFXChangedHandler);
        }

        private void Start()
        {
            // init values from the save file
            var saveData = SaveAndLoad.Load();

            sensibilitySlider.Value = saveData.Camera.sensibility;
            smoothnessSlider.Value = saveData.Camera.smoothness;
            masterSlider.Value = saveData.Sound.masterVolume;
            musicSlider.Value = saveData.Sound.musicVolume;
            sfxSlider.Value = saveData.Sound.sfxVolume;
        }

        public void OnSensibilityChangedHandler(float value)
        {
            OnCameraSensibilityChanged.NotifyAll(value);
            
            // save value
            var saveData = SaveAndLoad.Load();

            saveData.Camera.sensibility = value;
            SaveAndLoad.Save(saveData);
        }
        
        public void OnSmoothnessChangedHandler(float value)
        {
            OnCameraSmoothnessChanged.NotifyAll(value);
            
            // Save value
            var saveData = SaveAndLoad.Load();

            saveData.Camera.smoothness = value;
            SaveAndLoad.Save(saveData);
        }
        
        public void OnMasterChangedHandler(float value)
        {
            OnMasterVolumeChanged.NotifyAll(value);
            
            // Save value
            var saveData = SaveAndLoad.Load();

            saveData.Sound.masterVolume = value;
            SaveAndLoad.Save(saveData);
        }
        
        public void OnMusicChangedHandler(float value)
        {
            OnMusicVolumeChanged.NotifyAll(value);
            
            // Save value
            var saveData = SaveAndLoad.Load();

            saveData.Sound.musicVolume = value;
            SaveAndLoad.Save(saveData);
        }
        
        public void OnSFXChangedHandler(float value)
        {
            OnSFXVolumeChanged.NotifyAll(value);
            
            // Save value
            var saveData = SaveAndLoad.Load();

            saveData.Sound.sfxVolume = value;
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
            
            OnMasterVolumeChanged.DetachAll();
            OnMusicVolumeChanged.DetachAll();
            OnSFXVolumeChanged.DetachAll();
        }
    }
}