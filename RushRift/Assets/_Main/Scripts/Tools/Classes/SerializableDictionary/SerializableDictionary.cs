using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace MyTools.Global
{
    [Serializable]

    public class SerializedDictionary<T1, T2> : ISerializationCallbackReceiver, IDictionary<T1, T2>
    {
        [Serializable]
        public struct DictionaryElement
        {
            public T1 key;
            public T2 value;
        }

        #region Public Properties

        public int Count => _dictionary.Count;
        public bool IsReadOnly => false;
        public ICollection<T1> Keys => _dictionary.Keys;
        public ICollection<T2> Values => _dictionary.Values;
        public ValueValidationDelegate ValueValidation { get; set; } = AnyValueValidation;

        #endregion

        // Define a delegate for validating the value
        public delegate bool ValueValidationDelegate(T2 value);
        
        [SerializeField] private List<DictionaryElement> data = new();
        private List<DictionaryElement> _cachedData = new();
        private Dictionary<T1, T2> _dictionary = new();

        #region Operators

        public T2 this[T1 key]
        {
            get => _dictionary[key];
            set
            {
                if (ValueValidation(value))
                {
                    _dictionary[key] = value;
                }
                else
                {
#if UNITY_EDITOR
                    Debug.LogWarning(
                        $"Invalid value '{value}' provided for key '{key}' in the SerializedDictionary. Value not set.");
#endif
                }
            }
        }


        #endregion
        
        #region Serialization Methods

        public void OnBeforeSerialize()
        {
            if (data == _cachedData) return;

            data.Clear();

            foreach (var kvp in _dictionary)
            {
                data.Add(new DictionaryElement()
                {
                    key = kvp.Key,
                    value = kvp.Value
                });
            }
            
            _cachedData = data; // this change is correct, but made it imposible to add new elements
        }

        public void OnAfterDeserialize()
        {
            _dictionary.Clear();
            
            for (var i = 0; i < data.Count; i++)
            {
                var d = data[i];
                
                if (d.key == null) continue;
                
                if (!_dictionary.ContainsKey(d.key))
                {
                    Debug.Log($"Key {d.key} Added to dictionary");
                    _dictionary.Add(d.key, d.value);
                }
                else
                {
#if UNITY_EDITOR
                    Debug.LogWarning($"Duplicate key '{data[i].key}' found in the SerializedDictionary. Skipping.");
#endif
                }
            }
        }

        #endregion
        
        #region Dictionary Methods

        public void Add(T1 key, T2 value)
        {
            if (!_dictionary.ContainsKey(key))
            {
                if (ValueValidation(value))
                {
                    _dictionary.Add(key, value);
                }
                else
                {
#if UNITY_EDITOR
                    Debug.LogWarning(
                        $"Invalid value '{value}' provided for key '{key}' in the SerializedDictionary. Entry not added.");
#endif
                }
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning($"Key '{key}' already exists in the SerializedDictionary. Skipping.");
#endif
            }
        }

        public void CopyTo(KeyValuePair<T1, T2>[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (arrayIndex < 0 || arrayIndex >= array.Length)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));

            if (array.Length - arrayIndex < _dictionary.Count)
                throw new ArgumentException(
                    "The number of elements in the source dictionary is greater than the available space from arrayIndex to the end of the destination array.");

            var i = arrayIndex;
            foreach (var kvp in _dictionary)
            {
                array[i++] = new KeyValuePair<T1, T2>(kvp.Key, kvp.Value);
            }
        }

        public bool ContainsKey(T1 key) => _dictionary.ContainsKey(key);
        public void Remove(T1 key) => _dictionary.Remove(key);
        public bool TryGetValue(T1 key, out T2 value) => _dictionary.TryGetValue(key, out value);
        public void Clear() => _dictionary.Clear();
        public void Add(KeyValuePair<T1, T2> item) => Add(item.Key, item.Value);

        public bool Contains(KeyValuePair<T1, T2> item) => _dictionary.TryGetValue(item.Key, out var value) &&
                                                           EqualityComparer<T2>.Default.Equals(value, item.Value);

        public bool Remove(KeyValuePair<T1, T2> item) =>
            (_dictionary as ICollection<KeyValuePair<T1, T2>>).Remove(item);

        public IEnumerator<KeyValuePair<T1, T2>> GetEnumerator() => _dictionary.GetEnumerator();
        public Dictionary<T1, T2> GetDictionary() => _dictionary;
        public void SetDictionary(Dictionary<T1, T2> newDictionary) => _dictionary = newDictionary;

        
        IEnumerator IEnumerable.GetEnumerator() => _dictionary.GetEnumerator();

        private static bool AnyValueValidation(T2 value)
        {
            return value != null;
        }

        bool IDictionary<T1, T2>.Remove(T1 key)
        {
            return _dictionary.Remove(key);
        }
        
        [Conditional("UNITY_EDITOR")]
        public void EditorAdd(T1 key, T2 value)
        {
            data.Add(new DictionaryElement()
            {
                key = key,
                value = value,
            });
            _dictionary.Add(key, value);
        }

        #endregion
    }
}
