using System.Collections;
using System.Collections.Generic;
using Game.DesignPatterns.Observers;
using UnityEngine;

namespace Game.UI
{
    public abstract class UIAnimation : MonoBehaviour, IObserver
    {
        public void OnNotify()
        {
            Play();
        }

        public void Play()
        {
            Play(0);
        }
        public abstract void Play(float delay);
        public abstract IEnumerator PlayRoutine(float delay);
        public abstract void Stop();
        
        public void Dispose()
        {
            
        }
    }
}
