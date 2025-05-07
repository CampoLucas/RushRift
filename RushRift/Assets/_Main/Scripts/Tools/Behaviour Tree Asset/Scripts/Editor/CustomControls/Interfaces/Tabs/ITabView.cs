using UnityEngine.UIElements;

namespace BehaviourTreeAsset.EditorUI.VisualElements.Interfeces
{
    public interface ITabView
    {
        int TabCount { get; }
        int CurrentTab { get; }
        TabContainer Container { get; }
        TabContent Content { get; }

        bool AddTab<T>(string name, T element) where T : VisualElement;
        bool RemoveTab<T>(T element) where T : VisualElement;
        bool RemoveTab(int tabIndex);
        bool RemoveTab(string name);
        T GetTabElement<T>();
        T GetTabElement<T>(string name);
    }
}