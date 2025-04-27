using UnityEngine.UIElements;

namespace BehaviourTreeAsset.EditorUI.VisualElements.Interfeces
{
    public interface ITabContainer
    {
        void AddTab(TabButton button);
        void RemoveTab(TabButton button);
        void RemoveTab(int index);
        void Select(int index);
        void SelectAll();
        void Unselect(int index);
        void UnselectAll();

        void SetCurrentTab(int index);
    }
}