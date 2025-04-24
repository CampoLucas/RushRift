using System;

namespace Game
{
    public class FuncPredicate<T> : IPredicate<T>
    {
        private Func<T, bool> _condition;

        public FuncPredicate(Func<T, bool> condition)
        {
            _condition = condition;
        }
        
        public bool Evaluate(ref T pParams)
        {
            return _condition(pParams);
        }

        public void Dispose()
        {
            _condition = null;
        }
    }
}