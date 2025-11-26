using System;
using Game.DesignPatterns.Observers;
using Game.Saves;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class Options : MonoBehaviour
    {
        public static readonly ISubject<float> CameraSensibilityChanged = new Subject<float>();
        public static readonly ISubject<float> CameraSmoothnessChanged = new Subject<float>();
        public static readonly ISubject<float> MasterVolumeChanged = new Subject<float>();
        public static readonly ISubject<float> MusicVolumeChanged = new Subject<float>();
        public static readonly ISubject<float> SfxVolumeChanged = new Subject<float>();

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
            var saveData = SaveSystem.LoadSettings();

            sensibilitySlider.Value = saveData.Camera.sensibility;
            smoothnessSlider.Value = saveData.Camera.smoothness;
            masterSlider.Value = saveData.Sound.masterVolume;
            musicSlider.Value = saveData.Sound.musicVolume;
            sfxSlider.Value = saveData.Sound.sfxVolume;
        }

        public void OnSensibilityChangedHandler(float value)
        {
            CameraSensibilityChanged.NotifyAll(value);
            
            // save value
            var saveData = SaveSystem.LoadSettings();

            saveData.Camera.sensibility = value;
            SaveSystem.SaveSettings(saveData);
        }
        
        public void OnSmoothnessChangedHandler(float value)
        {
            CameraSmoothnessChanged.NotifyAll(value);
            
            // Save value
            var saveData = SaveSystem.LoadSettings();

            saveData.Camera.smoothness = value;
            SaveSystem.SaveSettings(saveData);
        }
        
        public void OnMasterChangedHandler(float value)
        {
            MasterVolumeChanged.NotifyAll(value);
            
            // Save value
            var saveData = SaveSystem.LoadSettings();

            saveData.Sound.masterVolume = value;
            SaveSystem.SaveSettings(saveData);
        }
        
        public void OnMusicChangedHandler(float value)
        {
            MusicVolumeChanged.NotifyAll(value);
            
            // Save value
            var saveData = SaveSystem.LoadSettings();

            saveData.Sound.musicVolume = value;
            SaveSystem.SaveSettings(saveData);
        }
        
        public void OnSFXChangedHandler(float value)
        {
            SfxVolumeChanged.NotifyAll(value);
            
            // Save value
            var saveData = SaveSystem.LoadSettings();

            saveData.Sound.sfxVolume = value;
            SaveSystem.SaveSettings(saveData);
        }

        private void OnDestroy()
        {
            sensibilitySlider.OnValueChanged.RemoveAllListeners();
            smoothnessSlider.OnValueChanged.RemoveAllListeners();
            masterSlider.OnValueChanged.RemoveAllListeners();
            musicSlider.OnValueChanged.RemoveAllListeners();
            sfxSlider.OnValueChanged.RemoveAllListeners();
            
            if (_instance == this)
            {
                _instance = null;
            }
            else
            {
                return;
            }
            
            // CameraSensibilityChanged.DetachAll();
            // CameraSmoothnessChanged.DetachAll();
            // MasterVolumeChanged.DetachAll();
            // MusicVolumeChanged.DetachAll();
            // SfxVolumeChanged.DetachAll();
        }
    }
}