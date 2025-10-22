using Game.Levels;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine.UIElements;

namespace Tools.PlayHook
{
    public class PlayLevelSelectorDock : EditorWindow
    {
        private PlayLevelToolbar _toolbar;
        private EditorToolbarDropdown _levelDropdown;
        private EditorToolbarButton _playButton;
        private EditorToolbarButton _openSceneButton;
        private EditorToolbarDropdown _selectDropdown;
        private const string WindowPrefsKey = "PlayLevelSelectorDock.Open";

        [MenuItem("Play Level Toolbar/Open Window %#l")] // Ctrl+Shift+L
        public static void ShowWindow()
        {
            var window = GetWindow<PlayLevelSelectorDock>("Play Level Toolbar");
            window.Show();
            EditorPrefs.SetBool(WindowPrefsKey, true);
        }

        [InitializeOnLoadMethod]
        private static void AutoReopen()
        {
            if (EditorPrefs.GetBool(WindowPrefsKey, false))
            {
                EditorApplication.delayCall += () =>
                {
                    var wnd = GetWindow<PlayLevelSelectorDock>("Play Level Toolbar");
                    wnd.Show();
                };
            }
        }

        private void OnEnable()
        {
            // keep alive through domain reloads
            rootVisualElement.Clear();
            _toolbar = new PlayLevelToolbar();
            rootVisualElement.Add(_toolbar);
            rootVisualElement.style.flexDirection = FlexDirection.Column;
            rootVisualElement.style.paddingLeft = 6;
            rootVisualElement.style.paddingRight = 6;
            rootVisualElement.style.paddingTop = 4;
            rootVisualElement.style.paddingBottom = 4;

            _levelDropdown = _toolbar._levelDropdown;
            _playButton = _toolbar._playButton;
            _openSceneButton = _toolbar._openSceneButton;
            _selectDropdown = _toolbar._selectDropdown;

            EditorApplication.playModeStateChanged += OnPlayModeStateChange;
            PlayLevelSelectionBridge.OnSelectionChanged += RestoreSelectorHandler;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChange;
            PlayLevelSelectionBridge.OnSelectionChanged -= RestoreSelectorHandler;
            EditorPrefs.SetBool(WindowPrefsKey, false);
        }

        private void OnPlayModeStateChange(PlayModeStateChange state)
        {
            // keep window visible across play/edit
            if (state == PlayModeStateChange.EnteredPlayMode ||
                state == PlayModeStateChange.ExitingPlayMode)
            {
                EditorPrefs.SetBool(WindowPrefsKey, true);
            }
        }
        
        private void RestoreSelectorHandler()
        {
            if (_toolbar != null) _toolbar.RestoreSelectorHandler();
            
            return;
            var path = PlayLevelSelectionBridge.GetLevelPath();
            if (!string.IsNullOrEmpty(path))
            {
                var lvl = AssetDatabase.LoadAssetAtPath<BaseLevelSO>(path);
                if (lvl != null && _levelDropdown != null)
                {
                    _levelDropdown.text = $"{lvl.LevelID:D2}: {lvl.LevelName}";
                }
            }
        }
    }
}