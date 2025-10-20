using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;

[Overlay(typeof(SceneView), "Play Level Controls")]
public class PlayLevelToolbarOverlay : ToolbarOverlay
{
    public PlayLevelToolbarOverlay() : base(PlayLevelToolbar.ID) { }
}