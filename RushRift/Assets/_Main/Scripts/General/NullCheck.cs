using System;
using System.Runtime.CompilerServices;
using Game.Entities;
using Game.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game
{
    /// <summary>
    /// A lightweight value-type wrapper that safely holds a nullable reference
    /// while tracking whether it has ever been assigned a valid value.
    /// </summary>
    /// /// <remarks>
    /// Unlike a normal reference, <see cref="NullCheck{T}"/> explicitly distinguishes between:
    /// <list type="bullet">
    /// <item><description>Never assigned (<see cref="HasValue"/> is false)</description></item>
    /// <item><description>Assigned to a valid object (managed or UnityEngine.Object)</description></item>
    /// <item><description>Assigned but now missing/destroyed (Unity fake null)</description></item>
    /// </list>
    /// This is particularly useful in Unity where <see cref="UnityEngine.Object"/> types
    /// can behave as null even when not technically null in memory.
    /// </remarks>
    /// <typeparam name="T">Reference type to track. Typically a UnityEngine.Object or class.</typeparam>
    public struct NullCheck<T> : IDisposable where T : class
    {
        /// <summary>
        /// Indicates whether the reference has ever been assigned a non-null value.
        /// </summary>
        public bool HasValue { get; private set; }
        private T _value;
        private int _cachedHash;
        private bool _isObject;

        /// <summary>
        /// Initializes a new <see cref="NullCheck{T}"/> with the specified value.
        /// </summary>
        /// <param name="value">Reference to store.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NullCheck(T value) : this()
        {
            Set(value);
        }

        /// <summary>
        /// Initializes a new <see cref="NullCheck{T}"/> with a value or a fallback default.
        /// </summary>
        /// <param name="value">Reference to store. If null, <paramref name="defaultValue"/> is used.</param>
        /// <param name="defaultValue">Fallback value if <paramref name="value"/> is null.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NullCheck(T value, Func<T> defaultValue) : this()
        {
            Set(value, defaultValue);
        }

        /// <summary>
        /// Attempts to get the stored value.
        /// </summary>
        /// <param name="value">Output value if available; otherwise <c>default</c>.</param>
        /// <returns>True if the value exists; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGet(out T value)
        {
            value = default;
            if (!HasValue) return false;

            value = _value;
            return true;
        }
        
        /// <summary>
        /// Attempts to get the stored value, if it doesn't it attemtps to get one.
        /// </summary>
        /// <param name="value">Output value if available; otherwise <c>default</c>.</param>
        /// <returns>True if the value exists; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGet(out T value, Func<T> fallback)
        {
            value = GetOrDefault(fallback);
            return HasValue;
        }

        /// <summary>
        /// Gets the stored value directly.
        /// </summary>
        /// <returns>The underlying reference (can be null if not initialized).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get() => _value;

        /// <summary>
        /// Gets the stored value, or uses a fallback generator if missing.
        /// </summary>
        /// <param name="fallback">A delegate that produces a replacement value if missing.</param>
        /// <returns>The stored or generated value.</returns>
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

        /// <summary>
        /// Sets the internal reference and updates hash information.
        /// </summary>
        /// <param name="value">The new value to assign.</param>
        /// <returns>True if the value is non-null; otherwise false.</returns>
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

        /// <summary>
        /// Sets the internal reference, using a default if the main value is null.
        /// </summary>
        /// <param name="value">Primary value.</param>
        /// <param name="defaultValue">Fallback value if <paramref name="value"/> is null.</param>
        /// <returns>True if a valid value was assigned; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Set(T value, Func<T> defaultValue)
        {
            return Set(value ?? defaultValue());
        }

        #region Operators

        /// <summary>
        /// Converts the <see cref="NullCheck{T}"/> to a boolean, returning <see cref="HasValue"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator bool(NullCheck<T> checker) => checker.HasValue;
        
        /// <summary>
        /// Implicitly wraps a reference into a <see cref="NullCheck{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator NullCheck<T>(T value) => new (value);
        
        /// <summary>
        /// Implicitly unwraps the stored value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator T(NullCheck<T> checker) => checker.Get();
        
        /// <summary>
        /// Compares two <see cref="NullCheck{T}"/> instances for equality.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static bool operator ==(NullCheck<T> left, NullCheck<T> right)
        {
            if (!left.HasValue && !right.HasValue)
                return true;
            if (!left.HasValue || !right.HasValue)
                return false;
            return Equals(left._value, right._value);
        }

        /// <summary>
        /// Compares two <see cref="NullCheck{T}"/> instances for inequality.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(NullCheck<T> left, NullCheck<T> right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Compares a <see cref="NullCheck{T}"/> and a direct reference for equality.
        /// </summary>
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

        /// <summary>
        /// Compares a direct reference and a <see cref="NullCheck{T}"/> for equality.
        /// </summary>
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

        /// <summary>
        /// Returns the cached hash code of the stored object.
        /// </summary>
        /// <remarks>
        /// - Returns 0 if no value has been assigned.  
        /// - For <see cref="UnityEngine.Object"/> types, uses <see cref="Object.GetInstanceID"/>.  
        /// - Returns 0 if the Unity object has been destroyed (missing reference).  
        /// </remarks>
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

        /// <summary>
        /// Clears the current value and cached data.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            _value = default;
            HasValue = false;
            _cachedHash = 0;
            _isObject = false;
        }
        
        /// <summary>
        /// Disposes the underlying value if it implements <see cref="IDisposable"/>,
        /// then resets this <see cref="NullCheck{T}"/>.
        /// </summary>
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