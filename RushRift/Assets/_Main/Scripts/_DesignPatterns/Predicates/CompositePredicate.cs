using System;

namespace Game
{
    public class CompositePredicate<T> : IPredicate<T>
    {
        private IPredicate<T>[] _predicates;

        public CompositePredicate(IPredicate<T>[] predicates)
        {
            _predicates = predicates;
        }
        
        public bool Evaluate(ref T args)
        {
            for (var i = 0; i < _predicates.Length; i++)
            {
                var p = _predicates[i];
                if (!p.Evaluate(ref args)) return false;
            }

            return true;
        }

        public void Dispose()
        {
            for (var i = 0; i < _predicates.Length; i++)
            {
                _predicates[i].Dispose();
            }
            
            _predicates = null;
        }
    }
}