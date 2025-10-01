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
    [DisallowMultipleComponent]
    public class AudioManager : MonoBehaviour
    {
        [Header("Sounds")]
        [SerializeField, Tooltip("All sounds available to play, including music.")]
        private Sound[] sounds;

        [Header("Audio Mixer")]
        [SerializeField, Tooltip("Global audio mixer used for volume control.")]
        private AudioMixer mixer;

        [Header("Persistence")]
        [SerializeField, Tooltip("If enabled, this AudioManager persists across scene loads.")]
        private bool keepAcrossScenes = true;
        
        [SerializeField, Tooltip("If true, the first AudioManager instance will ALWAYS persist across scenes (ignores other persistence toggles).")]
        private bool forcePersistAcrossScenes = true;

        [SerializeField, Tooltip("If true, the manager will automatically persist if any Sound is tagged as Music.")]
        private bool persistWhenMusicTaggedPresent = true;

        [Header("Debug")]
        [SerializeField, Tooltip("If enabled, prints logs about lifecycle and duplicates.")]
        private bool isDebugLoggingEnabled = false;

        private static AudioManager _instance;

        private Dictionary<string, Sound> _soundMap;
        private AudioSourcePool _pool;

        private ActionObserver<float> _onMasterVolumeChanged;
        private ActionObserver<float> _onMusicVolumeChanged;
        private ActionObserver<float> _onSFXVolumeChanged;

        private static GameObject s_musicGO;
        private static AudioSource s_musicSource;
        private static string s_currentMusicName;

        private readonly Dictionary<string, HashSet<AudioSource>> _activeSourcesByName = new Dictionary<string, HashSet<AudioSource>>(StringComparer.Ordinal);

        private void Awake()
        {
            bool anyMusicTagged = sounds != null && sounds.Any(s => s != null && s.IsMusic);
            bool shouldPersist = forcePersistAcrossScenes || keepAcrossScenes || (persistWhenMusicTaggedPresent && anyMusicTagged);

            if (_instance == null)
            {
                _instance = this;

                if (shouldPersist)
                {
                    if (transform.parent != null)
                        transform.SetParent(null, worldPositionStays: true); // ensure root so DontDestroyOnLoad is honored

                    DontDestroyOnLoad(gameObject);
                }

                EnsureMusicChannel();
            }
            else
            {
                if (isDebugLoggingEnabled) Debug.Log("[AudioManager] Duplicate found; destroying new instance.", this);
                Destroy(gameObject);
                return;
            }

            _soundMap = sounds.ToDictionary(s => s.Name, s => s, StringComparer.Ordinal);
            _pool = new AudioSourcePool(gameObject, GetComponents<AudioSource>());

            for (int i = 0; i < sounds.Length; i++)
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
            _onMusicVolumeChanged  = new ActionObserver<float>(OnMusicVolumeChanged);
            _onSFXVolumeChanged    = new ActionObserver<float>(OnSFXVolumeChanged);

            Options.OnMasterVolumeChanged.Attach(_onMasterVolumeChanged);
            Options.OnMusicVolumeChanged.Attach(_onMusicVolumeChanged);
            Options.OnSFXVolumeChanged.Attach(_onSFXVolumeChanged);
        }

        public static void Play(string name)
        {
            if (!_instance)
            {
                Debug.LogWarning("WARNING: AudioManager instance is null.");
                return;
            }

            if (!_instance._soundMap.TryGetValue(name, out var sound))
            {
                Debug.LogWarning($"WARNING: Sound '{name}' not found.");
                return;
            }

            if (!sound.CanPlay())
            {
                if (_instance.isDebugLoggingEnabled) Debug.Log($"[AudioManager] '{name}' skipped: timeBetweenPlays.");
                return;
            }

            _instance.PlaySound(sound);
            sound.RegisterPlayTime();
        }

        public static void Stop(string name)
        {
            if (!_instance)
            {
                Debug.LogWarning("WARNING: AudioManager instance is null.");
                return;
            }

            if (string.IsNullOrEmpty(name)) return;

            if (_instance.isDebugLoggingEnabled) Debug.Log($"[AudioManager] Stop requested for '{name}'.");

            if (s_musicSource && string.Equals(s_currentMusicName, name, StringComparison.Ordinal))
            {
                s_musicSource.Stop();
                s_currentMusicName = null;
            }

            if (_instance._activeSourcesByName.TryGetValue(name, out var set) && set != null && set.Count > 0)
            {
                var snapshot = ListCache;
                snapshot.Clear();
                foreach (var src in set) if (src) snapshot.Add(src);

                for (int i = 0; i < snapshot.Count; i++)
                {
                    var src = snapshot[i];
                    if (!src) continue;
                    src.Stop();
                    _instance._pool.Recycle(src);
                    _instance.UnregisterActiveSource(name, src);
                }
                snapshot.Clear();
            }
        }

        private static readonly List<AudioSource> ListCache = new List<AudioSource>(16);

        private void PlaySound(Sound sound)
        {
            if (sound.IsMusic)
            {
                PlayMusic(sound);
                return;
            }

            var source = _pool.Get();
            sound.Play(source);
            RegisterActiveSource(sound.Name, source);
            StartCoroutine(WaitForEnd(source, sound.Name, recycle: true));
        }

        private void PlayMusic(Sound sound)
        {
            EnsureMusicChannel();

            if (s_musicSource && string.Equals(s_currentMusicName, sound.Name, StringComparison.Ordinal))
            {
                if (!s_musicSource.isPlaying)
                    s_musicSource.UnPause();
                if (isDebugLoggingEnabled) Debug.Log($"[AudioManager] Music '{sound.Name}' already active; no restart.", this);
                return;
            }

            sound.Initialize(s_musicSource);
            var clipsField = typeof(Sound).GetField("clips", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var clips = (AudioClip[])clipsField.GetValue(sound);
            if (clips == null || clips.Length == 0)
            {
                Debug.LogWarning($"WARNING: Music sound '{sound.Name}' has no clips.");
                return;
            }
            var idx = clips.Length > 1 ? UnityEngine.Random.Range(0, clips.Length) : 0;
            s_musicSource.clip = clips[idx];
            s_musicSource.time = Mathf.Clamp(s_musicSource.time, 0f, s_musicSource.clip.length - 0.001f);
            s_currentMusicName = sound.Name;

            if (!s_musicSource.isPlaying)
                s_musicSource.Play();
        }

        private static void EnsureMusicChannel()
        {
            if (s_musicGO && s_musicSource) return;

            s_musicGO = GameObject.Find("_MusicChannel");
            if (!s_musicGO)
            {
                s_musicGO = new GameObject("_MusicChannel");
                GameObject.DontDestroyOnLoad(s_musicGO);
            }

            s_musicSource = s_musicGO.GetComponent<AudioSource>();
            if (!s_musicSource)
                s_musicSource = s_musicGO.AddComponent<AudioSource>();

            s_musicSource.playOnAwake = false;
            s_musicSource.loop = true;
        }

        private IEnumerator WaitForEnd(AudioSource source, string name, bool recycle)
        {
            yield return new WaitUntil(() => !source || !source.isPlaying);
            if (source) UnregisterActiveSource(name, source);
            if (recycle && source) _pool.Recycle(source);
        }

        private void RegisterActiveSource(string name, AudioSource source)
        {
            if (string.IsNullOrEmpty(name) || !source) return;
            if (!_activeSourcesByName.TryGetValue(name, out var set))
            {
                set = new HashSet<AudioSource>();
                _activeSourcesByName[name] = set;
            }
            set.Add(source);
        }

        private void UnregisterActiveSource(string name, AudioSource source)
        {
            if (string.IsNullOrEmpty(name) || !source) return;
            if (_activeSourcesByName.TryGetValue(name, out var set))
            {
                set.Remove(source);
                if (set.Count == 0) _activeSourcesByName.Remove(name);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;

            StopAllCoroutines();

            if (sounds != null)
            {
                for (int i = 0; i < sounds.Length; i++)
                    sounds[i]?.Dispose();
                sounds = null;
            }

            if (_activeSourcesByName.Count > 0)
            {
                foreach (var kv in _activeSourcesByName)
                {
                    foreach (var src in kv.Value) if (src) src.Stop();
                }
                _activeSourcesByName.Clear();
            }

            _pool?.Dispose();
            _pool = null;

            var masterSubject = Options.OnMasterVolumeChanged;
            var musicSubject  = Options.OnMusicVolumeChanged;
            var sfxSubject    = Options.OnSFXVolumeChanged;

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
                if (sfxSubject != null) sfxSubject.Detach(_onSFXVolumeChanged);
                _onSFXVolumeChanged.Dispose();
            }
        }

        private void SetMasterVolume(float value) => mixer.SetFloat("MasterVolume",  Mathf.Log10(Mathf.Max(0.0001f, value)) * 20f);
        private void SetMusicVolume(float value)  => mixer.SetFloat("MusicVolume",   Mathf.Log10(Mathf.Max(0.0001f, value)) * 20f);
        private void SetSFXVolume(float value)    => mixer.SetFloat("GameplayVolume",Mathf.Log10(Mathf.Max(0.0001f, value)) * 20f);

        private void OnSFXVolumeChanged(float v)   => SetSFXVolume(v);
        private void OnMusicVolumeChanged(float v) => SetMusicVolume(v);
        private void OnMasterVolumeChanged(float v)=> SetMasterVolume(v);
    }
}