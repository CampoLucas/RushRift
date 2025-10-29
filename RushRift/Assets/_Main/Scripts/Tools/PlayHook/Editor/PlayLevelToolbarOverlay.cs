using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;

namespace Tools.PlayHook
{
    [Overlay(typeof(SceneView), "Play Level Controls")]
    public class PlayLevelToolbarOverlay : ToolbarOverlay
    {
        public PlayLevelToolbarOverlay() : base(PlayLevelToolbar.ID)
        {
        }
    }
}