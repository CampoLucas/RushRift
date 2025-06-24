using Game.DesignPatterns.Factory;
using Game.Entities;
using UnityEngine;

namespace Game
{
    public class VFXFactory : IFactory<VFXEmitter, VFXEmitterParams>
    {
        private VFXEmitter _product;

        public VFXFactory(VFXEmitter product)
        {
            _product = product;
        }
        
        public VFXEmitter Create()
        {
            return Object.Instantiate(_product);
        }

        public VFXEmitter[] Create(int quantity)
        {
            var products = new VFXEmitter[quantity];

            for (var i = 0; i < quantity; i++)
            {
                products[i] = Create();
            }

            return products;
        }
        
        public void Dispose()
        {
            _product = null;
        }
    }
}