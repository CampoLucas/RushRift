using System;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

namespace Game
{
    /// <summary>
    /// Represents a single sound configuration, including clips, volume, pitch, looping, and mixer settings.
    /// </summary>
    [System.Serializable]
    public class Sound : IDisposable
    {
        /// <summary>
        /// Returns either a fixed pitch or a randomized pitch based on the pitch range.
        /// </summary>
        public float Pitch => randomPitch ? Random.Range(pitchRange.x, pitchRange.y) : pitch;
        /// <summary>
        /// Returns either a fixed volume or a randomized volume based on the volume range.
        /// </summary>
        public float Volume => randomVolume ? Random.Range(volumeRange.x, volumeRange.y) : volume;
        /// <summary>
        /// Name used to reference this sound.
        /// </summary>
        public string Name => name;
        /// <summary>
        /// If the sound is played when the AudioManager is Initialized.
        /// </summary>
        public bool PlayOnAwake => playOnAwake;

        [Header("Settings")]
        [SerializeField] private string name;
        [SerializeField] private bool loop;
        [SerializeField] private bool playOnAwake;
        [SerializeField] private float delaySeconds;
        
        [Header("Audio")]
        [SerializeField] private AudioClip[] clips;
        [SerializeField] private AudioMixerGroup mixer;

        [Header("Reproduction")]
        [SerializeField] private bool randomVolume;
        [SerializeField] private float volume;
        [SerializeField] private Vector2 volumeRange;
        [SerializeField] private float pitch;
        [SerializeField] private bool randomPitch;
        [SerializeField] private Vector2 pitchRange;
        [SerializeField] private float timeBetweenPlays = 0f;

        private float _lastPlayTime = float.MinValue;
        
        /// <summary>
        /// Applies this sound's settings to the provided AudioSource.
        /// </summary>
        public void Initialize(AudioSource source)
        {
            if (mixer != null) source.outputAudioMixerGroup = mixer;
            
            source.volume = Volume;
            source.pitch = Pitch;
            source.loop = loop;
        }

        /// <summary>
        /// Initializes and plays the sound clip using the provided AudioSource.
        /// </summary>
        /// <param name="source">The AudioSource used to play the clip.</param>
        public void Play(AudioSource source)
        {
            if (clips == null || clips.Length == 0)
            {
                Debug.LogWarning($"WARNING: Sound '{name}' has no clips assigned.");
                return;
            }
            
            Initialize(source);
            
            var clipIndex = clips.Length > 1 ? Random.Range(0, clips.Length) : 0;
            source.clip = clips[clipIndex];
            source.pitch = Pitch;

            source.enabled = true;
            
            if (delaySeconds > 0)
            {
                source.PlayDelayed(delaySeconds);
            }
            else
            {
                source.Play();
            }
        }
        
        public bool CanPlay()
        {
            return Time.unscaledTime - _lastPlayTime >= timeBetweenPlays;
        }

        public void RegisterPlayTime()
        {
            _lastPlayTime = Time.unscaledTime;
        }

        /// <summary>
        /// Frees references to clip and mixer for garbage collection.
        /// </summary>
        public void Dispose()
        {
            clips = null;
            mixer = null;
        }
    }
}