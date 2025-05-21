using System;
using UnityEngine;

namespace Game.Detection
{
    public interface IDetection : IDisposable
    {
        bool IsColliding { get; }
        int Overlaps { get; }
        
        bool Detect();
        void Draw(Transform origin, Color color);
    }
}