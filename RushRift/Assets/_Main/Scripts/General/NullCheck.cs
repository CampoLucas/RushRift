using System;

namespace Game
{
    public struct NullCheck<T> : IDisposable where T : class
    {
        private bool _hasValue;
        private T _value;

        public NullCheck(T value) : this()
        {
            Set(value);
        }

        public NullCheck(T value, T defaultValue) : this()
        {
            Set(value, defaultValue);
        }

        public bool HasValue() => _hasValue;

        public bool TryGetValue(out T value)
        {
            value = default;
            if (!_hasValue) return false;

            value = _value;
            return true;
        }

        public T Get() => _value;

        public void Set(T value)
        {
            _value = value;
            _hasValue = _value != null;
        }

        public void Set(T value, T defaultValue)
        {
            var v = value != null ? value : defaultValue;
            Set(v);
        }

        public bool TrySet(T value)
        {
            Set(value);
            return HasValue();
        }

        public bool TrySet(T value, T defaultValue)
        {
            Set(value, defaultValue);
            return HasValue();
        }
        
        public static implicit operator bool(NullCheck<T> checker) => checker.HasValue();
        public static implicit operator NullCheck<T>(T value) => new (value);
        public static implicit operator T(NullCheck<T> checker) => checker.Get();

        public void Dispose()
        {
            if (_value is IDisposable disposable) disposable.Dispose();
            _value = default;
            _hasValue = false;
        }
    }
}