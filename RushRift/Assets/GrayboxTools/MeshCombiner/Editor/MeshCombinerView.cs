using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace Game.Tools.MeshCombiner.Editor
{
    public class MeshCombinerView : GraphView
    {
        public new class UxmlFactory : UxmlFactory<MeshCombinerView, UxmlTraits>
        {
            
        }
    }
}