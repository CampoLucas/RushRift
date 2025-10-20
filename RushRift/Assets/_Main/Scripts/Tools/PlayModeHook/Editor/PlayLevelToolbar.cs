using System.Collections.Generic;
using System.Linq;
using Game.Levels;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

[EditorToolbarElement(ID, typeof(SceneView))]
public class PlayLevelToolbar : VisualElement
{
    public const string ID = "CustomToolbar/PlayLevel";
    public static readonly string DisabledFlag = "__NONE__";

    private static readonly string PrefKey = "PlayLevel.SelectedLevel";
    private static readonly string MainScenePath = "Assets/_Main/Scenes/MainScene.unity";
    private static readonly string MainMenuPath = "Assets/_Main/Scenes/Main Menu.unity";

    private EditorToolbarDropdown _levelDropdown;
    private EditorToolbarButton _playButton;
    private EditorToolbarButton _openSceneButton;
    private EditorToolbarButton _selectButton;
    
    private static List<BaseLevelSO> _levels = new();
    private static BaseLevelSO _selected;
    private static string _selectedScenePath;
    private static bool _isSceneOnly;

    public PlayLevelToolbar()
    {
        style.flexDirection = FlexDirection.Row;
        style.alignItems = Align.Center;
        style.paddingLeft = 2;
        style.paddingRight = 2;
        style.height = 22; 
        style.flexGrow = 0;

        // Compact dropdown
        _levelDropdown = new EditorToolbarDropdown();
        _levelDropdown.text = "(None)";
        _levelDropdown.tooltip = "Select Level or Scene to play";
        _levelDropdown.clicked += ShowLevelMenu;
        _levelDropdown.style.flexGrow = 0;
        _levelDropdown.style.flexShrink = 0;
        _levelDropdown.style.width = 100;
        _levelDropdown.style.height = 20;
        _levelDropdown.style.marginRight = 6;
        Add(_levelDropdown);

        // Play button
        _playButton = new EditorToolbarButton(OnPlayClicked)
        {
            text = "▶ Play",
            tooltip = "Play from MainScene with selected level"
        };
        _playButton.style.flexGrow = 0;
        _playButton.style.flexShrink = 0;
        _playButton.style.height = 20;
        _playButton.style.minWidth = 50;
        Add(_playButton);
        
        // Open Scene button
        _openSceneButton = new EditorToolbarButton(OnOpenSceneClicked)
        {
            text = "Open",
            tooltip = "Open the scene directly"
        };
        _openSceneButton.style.height = 20;
        _openSceneButton.style.minWidth = 35;
        _openSceneButton.style.marginLeft = 4;
        Add(_openSceneButton);
        
        // Select button
        _selectButton = new EditorToolbarButton(OnSelectClicked)
        {
            text = "Select",
            tooltip = "Ping and select the level asset or scene in Project"
        };
        _selectButton.style.height = 20;
        _selectButton.style.minWidth = 35;
        _selectButton.style.marginLeft = 4;
        Add(_selectButton);

        RefreshLevels();
        RestoreSelection();
        UpdateLevelButtonText();

        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        UpdatePlayModeVisuals(EditorApplication.isPlaying);
    }
    
    ~PlayLevelToolbar()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    private void ShowLevelMenu()
    {
        RefreshLevels();
        var menu = new GenericMenu();
        
        // None option (disables tool)
        menu.AddItem(new GUIContent("Nothing"), !_isSceneOnly && _selected == null, () =>
        {
            _selected = null;
            _isSceneOnly = false;
            
            // Save persistent flag that means "disabled"
            EditorPrefs.SetString(PrefKey, DisabledFlag);

            // Clear playModeStartScene so regular play uses active scene
            EditorSceneManager.playModeStartScene = null;
            
            SaveSelection();
            UpdateLevelButtonText();
        });
        
        menu.AddSeparator("");
        
        
        if (_levels.Count == 0)
        {
            menu.AddDisabledItem(new GUIContent("No levels found"));
        }
        else
        {
            foreach (var level in _levels)
            {
                var category = level switch
                {
                    LevelSO => "Sectors",
                    LevelRushSO => "Rushes",
                    _ => "Other"
                };

                var label = $"{category}/{level.LevelID:D2}: {level.LevelName} ({level.GetType().Name})";
                var selected = !_isSceneOnly && _selected == level;
                
                menu.AddItem(new GUIContent(label), selected, () =>
                {
                    _selected = level;
                    _isSceneOnly = false;
                    SaveSelection();
                    UpdateLevelButtonText();
                });
            }
        }
        
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("Scenes/Main Scene"), _isSceneOnly && _selectedScenePath == MainScenePath, () =>
        {
            _isSceneOnly = true;
            _selectedScenePath = MainScenePath;
            _selected = null;
            SaveSelection();
            UpdateLevelButtonText();
        });
            
        menu.AddItem(new GUIContent("Scenes/Main Menu"), _isSceneOnly && _selectedScenePath == MainMenuPath, () =>
        {
            _isSceneOnly = true;
            _selectedScenePath = MainMenuPath;
            _selected = null;
            SaveSelection();
            UpdateLevelButtonText();
        });
        
        // Show under dropdown text
        var world = _levelDropdown.worldBound;
        menu.DropDown(new Rect(world.xMin, world.yMax, 0, 0));
    }

    private void RefreshLevels()
    {
#if false
        var guids = AssetDatabase.FindAssets("t:ScriptableObject");
        _levels = guids
            .Select(g => AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(g)))
            .OfType<BaseLevelSO>()
            .OrderBy(l => l.name)
            .ToList();
#else
        var guids = AssetDatabase.FindAssets($"t:{typeof(BaseLevelSO)}");
        _levels = guids
            .Select(g => AssetDatabase.LoadAssetAtPath<BaseLevelSO>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(l => l != null)
            .OrderBy(l => l.LevelID)
            .ToList();
#endif
    }

    private void UpdateLevelButtonText()
    {
        if (_isSceneOnly)
        {
            if (_selectedScenePath == MainScenePath)
                _levelDropdown.text = "Main Scene";
            else if (_selectedScenePath == MainMenuPath)
                _levelDropdown.text = "Main Menu";
            else
                _levelDropdown.text = "(Unknown Scene)";
            
            _playButton.SetEnabled(true);
            _openSceneButton.SetEnabled(true);
            _selectButton.SetEnabled(true);
            return;
        }
        else if (_selected)
        {
            var typeName = _selected.GetType().Name;
            var name = _selected.name;
            var display = name.Length > 22 ? name.Substring(0, 22) + "…" : name;
            _levelDropdown.text = $"{display}";
            
            _playButton.SetEnabled(true);

            var multi = _selected is CompositeLevelSO;
            _openSceneButton.SetEnabled(!multi);
            _selectButton.SetEnabled(true);
        }
        else
        {
            _levelDropdown.text = "Select Level";
            _playButton.SetEnabled(false);
            _openSceneButton.SetEnabled(false);
            _selectButton.SetEnabled(false);
        }
    }

    private void SaveSelection()
    {
        if (_isSceneOnly)
        {
            EditorPrefs.SetString(PrefKey, _selectedScenePath);
            return;
        }
        
        if (_selected == null)
        {
            EditorPrefs.SetString(PrefKey, DisabledFlag);
            return;
        }

        var path = AssetDatabase.GetAssetPath(_selected);
        EditorPrefs.SetString(PrefKey, path);
    }

    private void RestoreSelection()
    {
        var path = EditorPrefs.GetString(PrefKey, "");
        if (string.IsNullOrEmpty(path) || path == DisabledFlag)
        {
            _selected = null;
            _isSceneOnly = false;
            return;
        }
        
        if (path == MainScenePath || path == MainMenuPath)
        {
            _selected = null;
            _isSceneOnly = true;
            _selectedScenePath = path;
            return;
        }
        
        _selected = AssetDatabase.LoadAssetAtPath<BaseLevelSO>(path);
    }

    private void OnPlayClicked()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
            return;
        }
        
        if (_isSceneOnly)
        {
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(_selectedScenePath);
            EditorSceneManager.playModeStartScene = scene;
            EditorApplication.isPlaying = true;
            return;
        }

        if (_selected == null)
        {
            PlayLevelHandler.SetSelectedLevel(_selected);
            EditorUtility.DisplayDialog("No Level Selected", "Please select a BaseLevelSO first.", "OK");
            return;
        }
        
        // Always start from MainScene
        var mainScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath);
        if (mainScene) EditorSceneManager.playModeStartScene = mainScene;
        
        // handoff to runtime bridge
        PlayLevelHandler.SetSelectedLevel(_selected);
        EditorApplication.isPlaying = true;
    }
    
    private void OnOpenSceneClicked()
    {
        if (_isSceneOnly)
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(_selectedScenePath);
            if (!sceneAsset)
            {
                EditorUtility.DisplayDialog("Cannot Open Scene",
                    $"The Scene at the path: '{_selectedScenePath}' was not found", "OK");
                
                return;
            }
            EditorSceneManager.OpenScene(_selectedScenePath);
            return;
        }

        if (_selected == null) return;

        if (_selected.LevelCount() > 1)
        {
            EditorUtility.DisplayDialog("Cannot Open", "This LevelSO contains multiple scenes and cannot be opened directly.", "OK");
            return;
        }

        if (_selected is LevelSO level)
        {
            var path = $"Assets/_Main/Scenes/Levels/{level.SceneName}.unity";
            
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            if (!sceneAsset)
            {
                EditorUtility.DisplayDialog("Cannot Open Scene",
                    $"The Scene at the path: '{path}' was not found", "OK");
                
                return;
            }
            
            EditorSceneManager.OpenScene(path);
        }
    }
    
    private void OnSelectClicked()
    {
        if (_isSceneOnly)
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(_selectedScenePath);
            if (sceneAsset)
            {
                Selection.activeObject = sceneAsset;
                EditorGUIUtility.PingObject(sceneAsset);
            }
            else
            {
                EditorUtility.DisplayDialog("Cannot Select Scene",
                    $"The Scene at the path: '{_selectedScenePath}' was not found", "OK");
            }
            return;
        }

        if (_selected)
        {
            Selection.activeObject = _selected;
            EditorGUIUtility.PingObject(_selected);
        }
        else
        {
            EditorUtility.DisplayDialog("Cannot Select Level", $"The Level was not found", "OK");
        }
    }
    
    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        // Call update when entering or exiting play mode
        UpdatePlayModeVisuals(EditorApplication.isPlaying);
    }

    private void UpdatePlayModeVisuals(bool isPlaying)
    {
        if (isPlaying)
        {
            // Change Play button color while in play mode
            _openSceneButton.SetEnabled(false);
            //_selectButton.SetEnabled(false);
        }
        else
        {
            // Restore normal look
            _openSceneButton.SetEnabled(true);
            //_selectButton.SetEnabled(true);
        }
    }
}
