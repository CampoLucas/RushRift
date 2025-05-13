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
        public string Name => name;

        [Header("Settings")]
        [SerializeField] private string name;
        [SerializeField] private bool loop;
        [SerializeField] private bool playOnAwake;
        [SerializeField] private ulong delay;
        
        [Header("Audio")]
        [SerializeField] private AudioClip[] clips;
        [SerializeField] private AudioMixerGroup mixer;
        
        [Header("Reproduction")]
        [SerializeField] private float volume;
        [SerializeField] private float pitch;
        [SerializeField] private bool randomPitch;
        [SerializeField] private Vector2 pitchRange;

        private AudioSource _source;

        public void Initialize(GameObject gameObject)
        {
            _source = gameObject.AddComponent<AudioSource>();
            if (clips.Length > 0) _source.clip = clips[0];
            if (mixer != null) _source.outputAudioMixerGroup = mixer;
            
            _source.volume = volume;
            _source.pitch = Pitch;
            _source.loop = loop;
            _source.playOnAwake = playOnAwake;
        }

        public void Play()
        {
            if (clips.Length == 0)
            {
                Debug.Log("Length is 0 capitan");
                return;
            }
            var clipIndex = clips.Length > 1 ? Random.Range(0, clips.Length) : 0;
            _source.clip = clips[clipIndex];
            _source.pitch = Pitch;

            Debug.Log("Play audio");
            if (delay > 0)
            {
                _source.Play(delay);
            }
            else
            {
                _source.Play();
            }
        }

        public void Dispose()
        {
            clips = null;
            mixer = null;
            _source = null;
        }
    }
}