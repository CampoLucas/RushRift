using System;

namespace Game.DesignPatterns.Factory
{
    public interface IProduct<TData> : IDisposable
    {
        TData Data { get; }
    }
}