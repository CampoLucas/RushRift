using System.Collections;
using System.Collections.Generic;
using Game.DesignPatterns.Observers;
using UnityEngine;
using UnityEngine.Events;

namespace Game.UI
{
    public abstract class UIAnimation : MonoBehaviour, IObserver
    {
        public abstract UnityEvent OnPlaySequences();
        public abstract UnityEvent OnAllSequencesComplete();
        
        public void OnNotify()
        {
            Play();
        }

        public void Play()
        {
            Play(0);
        }

        public abstract void Reset();
        
        public abstract void Play(float delay);
        public abstract IEnumerator PlayRoutine(float delay);
        public abstract void Stop();
        
        public void Dispose()
        {
            
        }

        [ContextMenu("Editor Play"), System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void EditorPlay()
        {
            if (!Application.isPlaying) return;
            Play();
        }
    }
}
