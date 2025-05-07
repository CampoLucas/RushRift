using UnityEngine.UIElements;

public class SplitView : TwoPaneSplitView
{
    public new class UxmlFactory : UxmlFactory<SplitView, UxmlTraits>
    {
    }

    public SplitView()
    {
        name = "split-view";
    }

    // public SplitView()
    // {
    //     var childA = new VisualElement()
    //     {
    //         name = "left-panel",
    //     };
    //
    //     var childB = new VisualElement()
    //     {
    //         name = "right-panel",
    //     };
    //     Add(childA);
    //     Add(childB);
    // }
}