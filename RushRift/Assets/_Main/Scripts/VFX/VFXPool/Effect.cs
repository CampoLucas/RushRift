using Game.DesignPatterns.Factory;
using Game.Entities;
using UnityEngine;

namespace Game
{
    public class Effect : IFactory<EffectEmitter, VFXEmitterParams>
    {
        private EffectEmitter _product;

        public Effect(EffectEmitter product)
        {
            _product = product;
        }
        
        public EffectEmitter Create()
        {
            return Object.Instantiate(_product);
        }

        public EffectEmitter[] Create(int quantity)
        {
            var products = new EffectEmitter[quantity];

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