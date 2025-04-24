using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Tools;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game
{
    [System.Serializable]
    public partial class SerializableSOCollection<T> : ICollection<T>
        where T : SerializableSO
    {
        public int Count => collection?.Count ?? -1;
        public bool IsReadOnly => false;
        
        public T this[int key]
        {
            get => GetValue(key);
            set => SetValue(key, value);
        }
        
        [SerializeField] private List<T> collection = new();
        
        public IEnumerator<T> GetEnumerator()
        {
            return collection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(T item)
        {
            collection.Add(item);
        }

        public void Clear()
        {
            collection.Clear();
        }

        public bool Contains(T item)
        {
            return collection.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            collection.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return collection.Remove(item);
        }
        
        public void Dispose()
        {
            collection.Clear();
        }

        public List<T> Get() => collection;
        public List<T1> Get<T1>() => collection.Cast<T1>().ToList();
            
        private T GetValue(int key)
        {
            if (key > Count - 1 || key < 0) return null;
            return collection[key];
        }

        private void SetValue(int key, T value)
        {
            if (key > Count - 1 || key < 0) return;
            collection[key] = value;
        }
    }
    
    #if UNITY_EDITOR
    public partial class SerializableSOCollection<T> : ISerializableSOCollection
    {
        public void OpenSearchWindow(Action<Object> selectedCallback)
        {
            SearchWindowProvider.OpenSearchTypeWindow<T>((a) => OnItemSelectedCallback(a, selectedCallback));
        }

        public string PropertyName() => typeof(T).Name;
        
        private void OnItemSelectedCallback(Type type, Action<Object> selectedCallback)
        {
            if (selectedCallback != null)
            {
                var element = Create(type);
                Add(element);
                selectedCallback(element);
            }
        }

        private T Create(Type type)
        {
            var element = ScriptableObject.CreateInstance(type) as T;
            element.name = type.Name;
            // assign guid here
            return element;
        }
        
        public Object GetAtIndex(int index)
        {
            return this[index];
        }

        public void RemoveAt(int index)
        {
            Remove(this[index]);
        }
    }
    #endif
    
}
