using System;
using UnityEngine;

namespace Game.Tools
{
    [Serializable]
    public class EnumContainer<T2, T1>
        where T2 : Enum
    {
        [SerializeField][HideInInspector] private T1[] content = null;
        [SerializeField][HideInInspector] private T2 enumType;

        public T1 this[int i]
        {
            get => content[i];
            set => content[i] = value;
        }
        
        public T1 this[T2 key]
        {
            get => content[Convert.ToInt32(key)];
            set => content[Convert.ToInt32(key)] = value;
        }
        
        public int Lenght => content.Length;
        public T1[] GetContent() => content;
        public T2 GetEnum() => enumType;
    }
}