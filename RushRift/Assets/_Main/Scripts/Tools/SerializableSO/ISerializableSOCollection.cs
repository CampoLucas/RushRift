
using UnityEngine;

namespace Game
{
    public interface ISerializableSOCollection
    {
        int Count { get; }
        void OpenSearchWindow(System.Action<Object> selectedCallback);
        string PropertyName();
        Object GetAtIndex(int index);
        void RemoveAt(int index);

    }
}