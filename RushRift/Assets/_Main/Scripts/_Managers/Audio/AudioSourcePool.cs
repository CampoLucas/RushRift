using System;
using System.Collections.Generic;
using System.Linq;
using Game.DesignPatterns.Pool;
using UnityEngine;

namespace Game
{
    
    /// <summary>
    /// A pool for AudioSource Components, with support for runtime creation and editor-assigned sources.
    /// </summary>
    public class AudioSourcePool : IPool<AudioSource>, IDisposable
    {
        private GameObject _ownerGameObject;
        private List<AudioSource> _availableSources = new();
        private List<AudioSource> _inUseSources = new();
        private HashSet<AudioSource> _runtimeCreated = new();

        private AudioSourcePool(GameObject owner)
        {
            _ownerGameObject = owner;
        }
        
        /// <summary>
        /// Initializes the pool using a set of preconfigured AudioSources (e.g., from the inspector).
        /// </summary>
        /// <param name="owner">The GameObject that will have all the AudioSources.</param>
        /// <param name="initialSources">A list of AudioSources to preload into the pool.</param>
        public AudioSourcePool(GameObject owner, IEnumerable<AudioSource> initialSources) : this(owner)
        {
            foreach (var source in initialSources)
            {
                if (source == null) continue;{}

                source.enabled = false;
                _availableSources.Add(source);
            }
        }

        /// <summary>
        /// Initializes the pool with a given number of new AudioSources created at runtime.
        /// </summary>
        /// <param name="owner">The GameObject that will have all the AudioSources.</param>
        /// <param name="initialSize">The number of AudioSources to instantiate.</param>
        public AudioSourcePool(GameObject owner, int initialSize) : this(owner)
        {
            for (var i = 0; i < initialSize; i++)
            {
                var source = CreateSource();
                _availableSources.Add(source);
            }
        }

        /// <summary>
        /// Retrieves an available AudioSource from the pool, or creates one if none are available.
        /// </summary>
        /// <returns>An AudioSource ready to be used.</returns>
        public AudioSource Get()
        {
            AudioSource source;

            if (_availableSources.Count > 0)
            {
                source = _availableSources[^1];
                _availableSources.RemoveAt(_availableSources.Count - 1);
            }
            else
            {
                source = CreateSource();
            }

            source.enabled = true;
            _inUseSources.Add(source);
            return source;
        }

        /// <summary>
        /// Returns an AudioSource back to the pool, stopping playback and clearing the clip.
        /// </summary>
        /// <param name="source">The AudioSource to recycle.</param>
        public void Recycle(AudioSource source)
        {
            if (source == null || !_inUseSources.Contains(source)) return;
            
            source.Stop();
            source.clip = null;
            source.enabled = false;

            _inUseSources.Remove(source);
            _availableSources.Add(source);
        }

        /// <summary>
        /// Completely removes an AudioSource from the pool and destroys it if it was created at runtime.
        /// </summary>
        /// <param name="source">The AudioSource to remove and destroy.</param>
        public void Remove(AudioSource source)
        {
            if (source == null) return;
            
            _inUseSources.Remove(source);
            _availableSources.Remove(source);
            
            if (_runtimeCreated.Contains(source))
            {
                UnityEngine.Object.Destroy(source);
                _runtimeCreated.Remove(source);
            }
        }

        /// <summary>
        /// Disposes of all runtime-created AudioSources and clears the pool.
        /// </summary>
        public void Dispose()
        {
            foreach (var source in _runtimeCreated)
            {
                if (source != null)
                    UnityEngine.Object.Destroy(source);
            }
            
            _availableSources.Clear();
            _inUseSources.Clear();
            _runtimeCreated.Clear();

            _ownerGameObject = null;
        }

        /// <summary>
        /// Creates a new AudioSource attached to the owner GameObject and registers it for cleanup.
        /// </summary>
        /// <returns>The newly created AudioSource.</returns>
        private AudioSource CreateSource()
        {
            var source = _ownerGameObject.AddComponent<AudioSource>();
            _runtimeCreated.Add(source);
            source.enabled = false;
            return source;
        }
    }
}