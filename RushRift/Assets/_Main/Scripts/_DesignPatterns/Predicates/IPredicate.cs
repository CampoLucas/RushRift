using System;

namespace Game
{
    public interface IPredicate<T> : IDisposable
    {
        bool Evaluate(ref T args);
    }

    public interface IPredicate : IDisposable
    {
        bool Evaluate();
    }
}