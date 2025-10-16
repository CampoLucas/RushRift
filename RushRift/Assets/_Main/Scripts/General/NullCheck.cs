using System;
using System.Runtime.CompilerServices;
using Game.Utils;
using Object = UnityEngine.Object;

namespace Game
{
    public struct NullCheck<T> : IDisposable where T : class
    {
        public bool HasValue { get; private set; }
        private T _value;
        private int _cachedHash;
        private bool _isObject;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NullCheck(T value) : this()
        {
            Set(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NullCheck(T value, T defaultValue) : this()
        {
            Set(value, defaultValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGet(out T value)
        {
            value = default;
            if (!HasValue) return false;

            value = _value;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get() => _value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetOrDefault(Func<T> fallback)
        {
            if (!TryGet(out var value))
            {
                value = fallback();
                Set(value);
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Set(T value)
        {
            _value = value;
            HasValue = _value != null;

            if (HasValue)
            {
                if (_value is UnityEngine.Object obj)
                {
                    _isObject = true;
                    _cachedHash = obj.GetInstanceID();
                }
                else
                {
                    _isObject = false;
                    _cachedHash = _value.GetHashCode();
                }
            }
            else
            {
                _isObject = false;
                _cachedHash = 0;
            }

            return HasValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Set(T value, T defaultValue)
        {
            return Set(value ?? defaultValue);
        }

        #region Operators

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator bool(NullCheck<T> checker) => checker.HasValue;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator NullCheck<T>(T value) => new (value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator T(NullCheck<T> checker) => checker.Get();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static bool operator ==(NullCheck<T> left, NullCheck<T> right)
        {
            if (!left.HasValue && !right.HasValue)
                return true;
            if (!left.HasValue || !right.HasValue)
                return false;
            return Equals(left._value, right._value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(NullCheck<T> left, NullCheck<T> right)
        {
            return !(left == right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(NullCheck<T> left, T right)
        {
            if (!left.HasValue) return right == null;
            return Equals(left._value, right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(NullCheck<T> left, T right)
        {
            return !(left == right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(T left, NullCheck<T> right)
        {
            if (!right.HasValue) return left == null;
            return Equals(left, right._value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(T left, NullCheck<T> right)
        {
            return !(left == right);
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            if (obj is NullCheck<T> other)
                return this == other;
            if (obj is T t)
                return this == t;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            if (!HasValue)
            {
                return 0;
            }

            if (_value == null || (_isObject && _value.IsNullOrMissingReference()))
            {
                return 0;
            }

            return _cachedHash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            _value = default;
            HasValue = false;
            _cachedHash = 0;
            _isObject = false;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (_value is IDisposable disposable)
            {
                disposable.Dispose();
            }
            Reset();
        }
    }
}