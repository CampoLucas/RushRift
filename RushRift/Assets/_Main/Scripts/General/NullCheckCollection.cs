using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Game
{
    public struct NullCheckCollection<T> : ICollection<T>
    {
        private ICollection<T> _value;
        private bool _hasValue;

        public NullCheckCollection(ICollection<T> collection) : this()
        {
            Set(collection);
        }
        
        public NullCheckCollection(ICollection<T> collection, ICollection<T> defaultCollection) : this()
        {
            Set(collection, defaultCollection);
        }

        public static implicit operator bool(NullCheckCollection<T> checker) => !checker.IsNullOrEmpty();
        
        public bool IsNullOrEmpty() => !_hasValue || _value.Count == 0;
        
        public bool HasValue() => _hasValue;

        public bool TryGetValue(out ICollection<T> value)
        {
            value = default;
            if (!_hasValue) return false;

            value = _value;
            return true;
        }

        public ICollection<T> Get() => _value;
        
        
        public void Set(ICollection<T> collection)
        {
            if (collection == null)
            {
                _value = null;
                _hasValue = false;
                return;
            }
            _value = collection;
            _hasValue = true;
        }

        public void Set(ICollection<T> collection, ICollection<T> defaultCollection)
        {
            var isOriginalNull = collection == null;
            if (collection != null)
            {
                _value = collection;
                _hasValue = true;
            }
            else if (defaultCollection != null)
            {
                _value = defaultCollection;
                _hasValue = true;
            }
            else
            {
                _value = null;
                _hasValue = false;
            }
        }

        public bool TrySet(ICollection<T> value)
        {
            Set(value);
            return HasValue();
        }

        public IEnumerator<T> GetEnumerator() => !HasValue() ? new List<T>().GetEnumerator() : _value.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => !HasValue() ? new List<T>().GetEnumerator() : _value.GetEnumerator();

        public void Add(T item)
        {
            if (!HasValue()) return;
            _value.Add(item);
        }

        public void Clear()
        {
            if (!HasValue()) return;
            _value.Clear();
        }

        public bool Contains(T item)
        {
            if (IsNullOrEmpty()) return false;
            return _value.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (!HasValue()) return;
            _value.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            if (IsNullOrEmpty()) return false;
            return _value.Remove(item);
        }

        public int Count => !HasValue() ? -1 : _value.Count;
        public bool IsReadOnly => HasValue() && _value.IsReadOnly;
        
    }
}