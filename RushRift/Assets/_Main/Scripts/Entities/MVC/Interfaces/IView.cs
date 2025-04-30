using System;
using UnityEngine;

namespace Game.Entities
{
    public interface IView : IDisposable
    {
        void Init(Animator[] animator);
        void Play(string name);
    }
}