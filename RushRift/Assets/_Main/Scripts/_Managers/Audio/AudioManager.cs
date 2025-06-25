using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.DesignPatterns.Observers;
using Game.UI;
using UnityEngine;
using UnityEngine.Audio;
namespace Game
{
    /// <summary>
    /// Manages the playback of audio in the game using pooled AudioSources.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        /// <summary>
        /// List of available sounds configured in the inspector.
        /// </summary>
        [SerializeField] private Sound[] sounds;

        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer mixer;

        private static AudioManager _instance;
        private Dictionary<string, Sound> _soundMap;
        private AudioSourcePool _pool;

        private ActionObserver<float> _onMasterVolumeChanged;
        private ActionObserver<float> _onMusicVolumeChanged;
        private ActionObserver<float> _onSFXVolumeChanged;

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
            
            //DontDestroyOnLoad(gameObject);

            _soundMap = sounds.ToDictionary(s => s.Name, s => s);
            _pool = new AudioSourcePool(gameObject, GetComponents<AudioSource>());

            for (var i = 0; i < sounds.Length; i++)
            {
                var sound = sounds[i];
                if (sound == null || !sound.PlayOnAwake) continue;
                
                PlaySound(sound);
            }
        }

        private void Start()
        {
            var saveData = SaveAndLoad.Load();
            var sound = saveData.Sound;
            
            SetMasterVolume(sound.masterVolume);
            SetMusicVolume(sound.musicVolume);
            SetSFXVolume(sound.sfxVolume);
            
            _onMasterVolumeChanged = new ActionObserver<float>(OnMasterVolumeChanged);
            _onMusicVolumeChanged = new ActionObserver<float>(OnMusicVolumeChanged);
            _onSFXVolumeChanged = new ActionObserver<float>(OnSFXVolumeChanged);

            Options.OnMasterVolumeChanged.Attach(_onMasterVolumeChanged);
            Options.OnMusicVolumeChanged.Attach(_onMusicVolumeChanged);
            Options.OnSFXVolumeChanged.Attach(_onSFXVolumeChanged);
        }

        /// <summary>
        /// Plays a sound by name if it exists in the audio manager.
        /// </summary>
        /// <param name="name">The name of the sound to play.</param>
        public static void Play(string name)
        {
            if (!_instance)
            {
                Debug.LogWarning($"WARNING: Instance is null.");
                return;
            }

            if (!_instance._soundMap.TryGetValue(name, out var sound))
            {
                Debug.LogWarning($"WARNING: Sound '{name}' not found.");
                return;
            }

            if (!sound.CanPlay())
            {
                Debug.Log($"Sound '{name}' skipped due to TimeBetweenPlays restriction.");
                return;
            }

            _instance.PlaySound(sound);
            sound.RegisterPlayTime();
        }


        /// <summary>
        /// Plays the given sound using a pooled audio source.
        /// </summary>
        /// <param name="sound">The sound data to play.</param>
        private void PlaySound(Sound sound)
        {
            var source = _pool.Get();
            Play(source, sound);
        }

        /// <summary>
        /// Initializes and plays the sound, then starts a coroutine to return the audio source to the pool.
        /// </summary>
        /// <param name="source">AudioSource to use for playback.</param>
        /// <param name="sound">Sound data to apply and play.</param>
        private void Play(AudioSource source, Sound sound)
        {
            sound.Play(source);
            StartCoroutine(WaitForEnd(source));
        }

        /// <summary>
        /// Coroutine that waits for the sound to finish playing before recycling the audio source.
        /// </summary>
        /// <param name="source">The audio source to monitor and recycle.</param>
        private IEnumerator WaitForEnd(AudioSource source)
        {
            yield return new WaitUntil(() => !source.isPlaying);
            _pool.Recycle(source);
        }
        
        /// <summary>
        /// Cleans up resources when the AudioManager is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
            
            StopAllCoroutines();

            for (var i = 0; i < sounds.Length; i++)
            {
                var sound = sounds[i];
                if (sound == null) continue;
                
                sound.Dispose();
            }

            sounds = null;
            
            _pool?.Dispose();
            _pool = null;


            var masterSubject = Options.OnMasterVolumeChanged;
            var musicSubject = Options.OnMusicVolumeChanged;
            var sfxSubject = Options.OnSFXVolumeChanged;

            if (_onMasterVolumeChanged != null)
            {
                if (masterSubject != null) masterSubject.Detach(_onMasterVolumeChanged);
                _onMasterVolumeChanged.Dispose();
            }
            
            if (_onMusicVolumeChanged != null)
            {
                if (musicSubject != null) musicSubject.Detach(_onMusicVolumeChanged);
                _onMusicVolumeChanged.Dispose();
            }
            
            if (_onSFXVolumeChanged != null)
            {
                if (sfxSubject != null) sfxSubject.Detach(_onMusicVolumeChanged);
                _onSFXVolumeChanged.Dispose();
            }
        }

        private void SetMasterVolume(float value)
        {
            mixer.SetFloat("MasterVolume", Mathf.Log10(value) * 20);
        }
        
        private void SetMusicVolume(float value)
        {
            mixer.SetFloat("MusicVolume", Mathf.Log10(value) * 20);
        }
        
        private void SetSFXVolume(float value)
        {
            mixer.SetFloat("GameplayVolume", Mathf.Log10(value) * 20);
        }
        
        private void OnSFXVolumeChanged(float obj)
        {
            SetSFXVolume(obj);
        }

        private void OnMusicVolumeChanged(float obj)
        {
            SetMusicVolume(obj);
        }

        private void OnMasterVolumeChanged(float obj)
        {
            SetMasterVolume(obj);
        }
    }
}