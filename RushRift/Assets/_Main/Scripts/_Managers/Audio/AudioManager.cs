using System;
using UnityEngine;

namespace Game
{
    public class AudioManager : MonoBehaviour
    {
        [SerializeField] private Sound[] sounds;

        private static AudioManager _instance;

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
            
            DontDestroyOnLoad(gameObject);

            foreach (var s in sounds)
            {
                s.Initialize(gameObject);
            }
        }

        public static void Play(string name)
        {
            if (!_instance) return;

            var s = Array.Find(_instance.sounds, sound => sound.Name == name);
            s.Play();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}