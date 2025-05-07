using UnityEngine.UIElements;

namespace BehaviourTreeAsset.EditorUI.VisualElements.Interfeces
{
    public interface ITabContent
    {
        VisualElement CurrentElement { get; }

        void AddTabElement<T>(T element) where T : VisualElement;
        void RemoveTabElement<T>(T element) where T : VisualElement;
        void SetCurrentElement(VisualElement element);
        T GetTabElement<T>() where T : VisualElement;
        bool TryGetTabElement<T>(out T element) where T : VisualElement;
    }
}