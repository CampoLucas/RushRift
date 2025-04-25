using System;

namespace Game.DesignPatterns.Factory
{
    public interface IFactory<out TProduct, TData> : IDisposable
        where TProduct : IProduct<TData>
    {
        //TProduct Product { get; }

        TProduct Create();
        TProduct[] Create(int quantity);
    }

    public interface IFactory<out TProduct> : IDisposable
    {
        //TProduct Product { get; }
        TProduct Create();
        TProduct[] Create(int quantity);
    }

    public interface IPrototypeFactory<out TProduct> : IDisposable, IFactory<TProduct>
        where TProduct : IPrototype<TProduct>
    {
        
    }
}