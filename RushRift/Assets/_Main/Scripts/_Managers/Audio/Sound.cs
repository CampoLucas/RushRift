using System;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

namespace Game
{
    [System.Serializable]
    public class Sound : IDisposable
    {
        public float Pitch => randomPitch ? Random.Range(pitchRange.x, pitchRange.y) : pitch;
        public float Volume => randomVolume ? Random.Range(volumeRange.x, volumeRange.y) : volume;
        public string Name => name;
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

        [Header("Tags")]
        [SerializeField, Tooltip("If true, this sound is treated as Music. The AudioManager will persist across scenes so this keeps playing.")]
        private bool isMusic = false;
        public bool IsMusic => isMusic;

        private float _lastPlayTime = float.MinValue;

        public void Initialize(AudioSource source)
        {
            if (mixer != null) source.outputAudioMixerGroup = mixer;
            source.volume = Volume;
            source.pitch = Pitch;
            source.loop = loop;
        }

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

            if (delaySeconds > 0) source.PlayDelayed(delaySeconds);
            else source.Play();
        }

        public bool CanPlay() => Time.unscaledTime - _lastPlayTime >= timeBetweenPlays;
        public void RegisterPlayTime() => _lastPlayTime = Time.unscaledTime;

        public void Dispose()
        {
            clips = null;
            mixer = null;
        }
    }
}
