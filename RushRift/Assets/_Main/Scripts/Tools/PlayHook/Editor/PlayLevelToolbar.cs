using System;
using System.Collections.Generic;
using System.Linq;
using Game.Levels;
using Game.Levels.SingleLevel;
using Game.Utils;
using Tools.PlayHook.Elements;
using Tools.PlayHook.Elements.Menu;
using Tools.PlayHook.Utils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.UIElements;
using MenuItem = Tools.PlayHook.Elements.Menu.MenuItem;
using Object = UnityEngine.Object;

namespace Tools.PlayHook
{
    [EditorToolbarElement(ID, typeof(SceneView))]

    public class PlayLevelToolbar : VisualElement
    {
        public const string ID = "CustomToolbar/PlayLevel";
        public static readonly string DisabledFlag = "__NONE__";

        private static readonly string LevelPrefKey = "PlayLevel.SelectedLevel";
        private static readonly string ModePrefKey = "PlayLevel.SelectedMode";
        private static readonly string MainScenePath = "Assets/_Main/Scenes/MainScene.unity";
        private static readonly string MainMenuPath = "Assets/_Main/Scenes/Main Menu.unity";

        public readonly OptionsButton _levelDropdown;
        public readonly EditorToolbarButton _playButton;
        public readonly OptionsButton _moreOptions;

        public class EditorButtonDropdown : EditorToolbarDropdown
        {
            public VisualElement Arrow { get; private set; }
            
            public EditorButtonDropdown(Action select) : base(select)
            {
                var children = Children();

                foreach (var child in children)
                {
                    if (child is Image or TextElement) continue;

                    Arrow = child;
                    Arrow.name = "arrow";
                }
            }
        }
        
        
        private static List<GameModeSO> _gameModes = new();
        private static List<BaseLevelSO> _levels = new();
        private static BaseLevelSO _selectedLevel;
        private static GameModeSO _selectedMode;
        private static string _selectedScenePath;
        private static bool _isSceneOnly;

        public PlayLevelToolbar() : this(true)
        {
            
        }
        
        public PlayLevelToolbar(bool isToolbar)
        {
            PlayLevelSelectionBridge.OnSelectionChanged += RestoreSelectorHandler;
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
            style.paddingLeft = 2;
            style.paddingRight = 2;
            style.height = 22;
            style.flexGrow = 0;

            // Compact dropdown
            _levelDropdown = new OptionsButton("Select Level", "level-selector", isToolbar)
            {
                name = "level-dropdown"
            };
            _levelDropdown.tooltip = "Select Level or Scene to play";
            _levelDropdown.RegisterCallback(OnLevelSelectorHandler, GetLevelsHandler);
            Add(_levelDropdown);

            // Play button
            _playButton = new EditorToolbarButton(OnPlayClicked)
            {
                name = "play-button",
                text = "▶",
                tooltip = "Play from MainScene with selected level"
            };
            _playButton.style.flexGrow = 0;
            _playButton.style.flexShrink = 0;
            _playButton.style.height = 20;
            _playButton.style.minWidth = 1;
            Add(_playButton);
            
            // More options button
            _moreOptions = new OptionsButton("…", "options-menu", isToolbar)
            {
                tooltip = "More options menu"
            };
            _moreOptions.RegisterCallback(OnOpenMenuHandler, GetOptionsHandler);
            
            Add(_moreOptions);

            RefreshAssets();
            RestoreSelection();
            UpdateLevelButtonText();

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            UpdatePlayModeVisuals(EditorApplication.isPlaying);
        }

        #region Level Selector

        private void OnLevelSelectorHandler(OptionsButton button)
        {
            RefreshAssets();
            ShowLevelMenu();
        }
        
        private List<MenuEntry> GetLevelsHandler()
        {
            var entries = new List<MenuEntry>();
            
            // None option (disables tool)
            entries.Add(new MenuItem("Nothing", SelectNothing, IsNothingOn, EnabledEntry));
            
            // Game modes
            entries.Add(new MenuSeparator());
            if (_gameModes.Count > 0)
            {
                foreach (var mode in _gameModes)
                {
                    var group = new MenuGroup(mode.DisplayName);
                    
                    // Try to get levels for mode
                    var modeLevels = mode.Levels;
                    if (modeLevels == null || modeLevels.Count == 0)
                    {
                        group.Add(new MenuItem("Empty", null, false, DisabledEntry));
                        entries.Add(group);
                        continue;
                    }

                    foreach (var lvl in modeLevels)
                    {
                        var label = $"{lvl.LevelID:D2}: {lvl.LevelName} ({lvl.GetType().Name})";
                        var selected = !_isSceneOnly && (_selectedMode == mode && _selectedLevel == lvl);
                        
                        group.Add(new MenuItem(label, () =>
                        {
                            _selectedMode = mode;
                            _selectedLevel = lvl;
                            _isSceneOnly = false;
                            SaveSelection();
                            UpdateLevelButtonText();
                        }, selected, EnabledEntry));
                    }
                    
                    entries.Add(group);
                }
            }
            else
            {
                entries.Add(new MenuItem("No GameModes found", null, false, DisabledEntry));
            }
            
            // all levels
            entries.Add(new MenuSeparator());
            if (_levels.Count == 0)
            {
                entries.Add(new MenuItem("No levels found", null, false, DisabledEntry));
            }
            else
            {
                var levels = new MenuGroup("All Levels");
                
                foreach (var lvl in _levels)
                {
                    var label = $"{lvl.LevelID:D2}: {lvl.LevelName} ({lvl.GetType().Name})";
                    var isSel = !_isSceneOnly && _selectedMode == null && _selectedLevel == lvl;
                    
                    levels.Add(new MenuItem(label, () =>
                    {
                        _selectedMode = null;
                        _selectedLevel = lvl;
                        _isSceneOnly = false;
                        SaveSelection();
                        UpdateLevelButtonText();
                    }, isSel, EnabledEntry));
                }
                
                entries.Add(levels);
            }
            
            // Scenes
            entries.Add(new MenuSeparator());
            var scenes = new MenuGroup("Scenes");
            
            scenes.Add(new MenuItem("Main Scene", () =>
            {
                _isSceneOnly = true;
                _selectedScenePath = MainScenePath;
                _selectedLevel = null;
                SaveSelection();
                UpdateLevelButtonText();
            },_isSceneOnly && _selectedScenePath == MainScenePath, EnabledEntry));
            
            scenes.Add(new MenuItem("Main Menu", () =>
            {
                _isSceneOnly = true;
                _selectedScenePath = MainMenuPath;
                _selectedLevel = null;
                SaveSelection();
                UpdateLevelButtonText();
            },_isSceneOnly && _selectedScenePath == MainMenuPath, EnabledEntry));
            
            entries.Add(scenes);
            return entries;
        }

        private bool IsNothingOn()
        {
            return !_isSceneOnly && !_selectedLevel;
        }

        private void SelectNothing()
        {
            _selectedLevel = null;
            _isSceneOnly = false;

            // Save persistent flag that means "disabled"
            EditorPrefs.SetString(LevelPrefKey, DisabledFlag);

            // Clear playModeStartScene so regular play uses active scene
            EditorSceneManager.playModeStartScene = null;

            SaveSelection();
            UpdateLevelButtonText();
        }

        #endregion
        
        #region Options

        private void OnOpenMenuHandler(OptionsButton button)
        {
            RefreshAssets();
        }

        private List<MenuEntry> GetOptionsHandler()
        {
            var entries = new List<MenuEntry>();
            
            RegularOptions(ref entries);
            SceneOptions(ref entries);
            SelectOptions(ref entries);
            ToggleMainSceneOption(ref entries);
            
            return entries;
        }

        private bool RegularOptions(ref List<MenuEntry> entries)
        {
            var group = new MenuGroup("Create");
            group.Add(new MenuItem("Create Level", () => {}, false, () => true));
            group.Add(new MenuItem("Create GameMode", () => {}, false, () => true));
            
            entries.Add(group);
            return true;
        }
        
        private bool SceneOptions(ref List<MenuEntry> entries, int maxScenesToCollapse = 3)
        {
            if (!_isSceneOnly && !_selectedLevel) return false;
            if (!_isSceneOnly && _selectedLevel.LevelCount() == 0) return false;
            
            entries.Add(new MenuSeparator());
            if (_isSceneOnly || (_selectedLevel && _selectedLevel.LevelCount() == 1))
            {
                if (_isSceneOnly)
                {
                    entries.Add(new MenuItem("Open Scene", () => OpenScene(_selectedScenePath), false, () => OpenSceneDisabled(_selectedScenePath)));
                }
                else
                {
                    var lvl = _selectedLevel.GetLevel(0);
                    entries.Add(new MenuItem($"Open {_selectedLevel.LevelName} Scene", () => OpenLevelScene(lvl), false, 
                        () => OpenSceneDisabled(lvl.ScenePath)));
                }
                return true;
            }

            var count = _selectedLevel.LevelCount();
            var scenes = new List<MenuEntry>();
            var collapsed = count > maxScenesToCollapse;
            
            for (var i = 0; i < count; i++)
            {
                var level = _selectedLevel.GetLevel(i);

                if (level.IsNullOrMissingReference())
                {
                    entries.Add(new MenuItem("Null level", null, false, DisabledEntry));
                    continue;
                }
                
                scenes.Add(new MenuItem(collapsed ? $"Open {level.LevelName}" : $"Open {level.LevelName} scene", () => OpenLevelScene(level), false,
                    () => OpenSceneDisabled(level.ScenePath)));
            }

            if (count > maxScenesToCollapse)
            {
                entries.Add(new MenuGroup("Scenes", scenes));
            }
            else
            {
                entries.AddRange(scenes);
            }

            return true;
        }
        
        private bool SelectOptions(ref List<MenuEntry> entries)
        {
            if (!_isSceneOnly && !_selectedLevel) return false;

            entries.Add(new MenuSeparator());
            if (_isSceneOnly)
            {
                entries.Add(new MenuItem("Select", () => PingSceneAtPath(_selectedScenePath), false, EnabledEntry));

                return true;
            }

            var group = new MenuGroup("Select");
            // ToDo: change it so it also shows the rushes scenes
            if (_selectedLevel is SingleLevelSO lvl)
            {
                var scenePath = lvl.ScenePath;
                group.Add(new MenuItem("Scene", () => PingSceneAtPath(scenePath), false, EnabledEntry));
            }
            
            // Add GameMode (if it exists)
            if (_selectedMode != null)
            {
                group.Add(new MenuItem("Game Mode", () => PingAsset(_selectedMode), false, EnabledEntry));
            }
            
            group.Add(new MenuItem("Level", () => PingAsset(_selectedLevel), false, EnabledEntry));

            entries.Add(group);
            return true;
        }

        private bool ToggleMainSceneOption(ref List<MenuEntry> entries)
        {
            if (!_selectedLevel) return false;
            entries.Add(new MenuSeparator());
            entries.Add(new MenuItem("Add Main Scene [DEBUG]", OnAddMainSceneClicked, IsMainSceneLoaded, OpenMainSceneDisabled));
            return true;
        }

        private bool OpenSceneDisabled(string path)
        {
            if (Application.isPlaying) return true;
            
            var scene = EditorSceneManager.GetSceneByPath(path);
            return scene.isLoaded;
        }

        private bool IsMainSceneLoaded()
        {
            return OpenSceneDisabled(MainScenePath);
        }

        private bool OpenMainSceneDisabled()
        {
            return Application.isPlaying;
        }
        
        private void OnAddMainSceneClicked()
        {
            // Only works for LevelSO (single-level scenes)
            var path = PlayLevelSelectionBridge.GetLevelPath();
            if (string.IsNullOrEmpty(path))
            {
                EditorUtility.DisplayDialog("No Level Selected", "Please select a LevelSO first.", "OK");
                return;
            }

            var levelAsset = AssetDatabase.LoadAssetAtPath<BaseLevelSO>(path);
            if (levelAsset is not LevelSO level)
            {
                EditorUtility.DisplayDialog("Unsupported Type", 
                    "You can only use 'Add Main Scene' with a LevelSO that represents a single scene.", "OK");
                return;
            }

            var levelScenePath = $"Assets/_Main/Scenes/Levels/{level.SceneName}.unity";
            var mainSceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath);

            if (mainSceneAsset == null)
            {
                EditorUtility.DisplayDialog("Main Scene Missing", 
                    $"The main scene at '{MainScenePath}' could not be found.", "OK");
                return;
            }

            // Check if main scene already open
            var mainScene = EditorSceneManager.GetSceneByPath(MainScenePath);
            if (mainScene.isLoaded)
            {
                EditorUtility.DisplayDialog("Main Scene Already Open",
                    "The MainScene is already loaded.", "OK");
                return;
            }

            // Open additively
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(MainScenePath, UnityEditor.SceneManagement.OpenSceneMode.Additive);
            Debug.Log($"[PlayLevelToolbar] Main Scene loaded additively into editor for preview with '{level.SceneName}'.");
        }

        private void PingSceneAtPath(string path)
        {
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            if (scene != null)
            {
                PingAsset(scene);
            }
            else
            {
                EditorUtility.DisplayDialog("Cannot Select", "The scene was not found.", "OK");
            }
        }
        
        private bool DisabledEntry() => true;
        private bool EnabledEntry() => false;

        private void OpenScene(string path)
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            if (!sceneAsset)
            {
                EditorUtility.DisplayDialog("Cannot Open Scene",
                    $"The Scene at the path: '{path}' was not found", "OK");

                return;
            }

            EditorSceneManager.OpenScene(path);
        }

        private void OpenLevelScene(SingleLevelSO level)
        {
            OpenScene(level.ScenePath);
        }

        #endregion
        

        public void RestoreSelectorHandler()
        {
            //RefreshAssets();
            RestoreSelection();
            UpdateLevelButtonText();
        }

        ~PlayLevelToolbar()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            PlayLevelSelectionBridge.OnSelectionChanged -= RestoreSelectorHandler;
        }

        private void ShowLevelMenu()
        {
            RefreshAssets();
            var menu = new GenericMenu();

            // None option (disables tool)
            menu.AddItem(new GUIContent("Nothing"), !_isSceneOnly && _selectedLevel == null, () =>
            {
                _selectedLevel = null;
                _isSceneOnly = false;

                // Save persistent flag that means "disabled"
                EditorPrefs.SetString(LevelPrefKey, DisabledFlag);

                // Clear playModeStartScene so regular play uses active scene
                EditorSceneManager.playModeStartScene = null;

                SaveSelection();
                UpdateLevelButtonText();
            });

            menu.AddSeparator("");

            // Game modes
            if (_gameModes.Count > 0)
            {
                foreach (var mode in _gameModes)
                {
                    // Try to get levels for mode
                    var modeLevels = mode.Levels;
                    if (modeLevels == null || modeLevels.Count == 0)
                    {
                        menu.AddDisabledItem(new GUIContent($"{mode.DisplayName}/Empty"));
                        continue;
                    }

                    foreach (var lvl in modeLevels)
                    {
                        var label = $"{mode.DisplayName}/{lvl.LevelID:D2}: {lvl.LevelName} ({lvl.GetType().Name})";
                        var selected = !_isSceneOnly && (_selectedMode == mode && _selectedLevel == lvl);

                        menu.AddItem(new GUIContent(label), selected, () =>
                        {
                            _selectedMode = mode;
                            _selectedLevel = lvl;
                            _isSceneOnly = false;
                            SaveSelection();
                            UpdateLevelButtonText();
                        });
                    }
                }
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("No GameModes found"));
            }

            // All Levels
            menu.AddSeparator("");
            if (_levels.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No levels found"));
            }
            else
            {
                foreach (var lvl in _levels)
                {
                    var label = $"All Levels/{lvl.LevelID:D2}: {lvl.LevelName} ({lvl.GetType().Name})";
                    var isSel = !_isSceneOnly && _selectedMode == null && _selectedLevel == lvl;
                    menu.AddItem(new GUIContent(label), isSel, () =>
                    {
                        _selectedMode = null;
                        _selectedLevel = lvl;
                        _isSceneOnly = false;
                        SaveSelection();
                        UpdateLevelButtonText();
                    });
                }
            }

            // Scenes
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Scenes/Main Scene"), _isSceneOnly && _selectedScenePath == MainScenePath, () =>
            {
                _isSceneOnly = true;
                _selectedScenePath = MainScenePath;
                _selectedLevel = null;
                SaveSelection();
                UpdateLevelButtonText();
            });

            menu.AddItem(new GUIContent("Scenes/Main Menu"), _isSceneOnly && _selectedScenePath == MainMenuPath, () =>
            {
                _isSceneOnly = true;
                _selectedScenePath = MainMenuPath;
                _selectedLevel = null;
                SaveSelection();
                UpdateLevelButtonText();
            });

            // Show under dropdown text
            var world = _levelDropdown.worldBound;
            menu.DropDown(new Rect(world.xMin, world.yMax, 0, 0));
        }

        private void RefreshAssets()
        {
            // Load all game modes
            var modeGuids = AssetDatabase.FindAssets($"t:{typeof(GameModeSO)}");
            _gameModes = modeGuids
                .Select(g => AssetDatabase.LoadAssetAtPath<GameModeSO>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(m => m != null)
                .OrderBy(m => m.name)
                .ToList();

            // Load all levels
            var levelGuids = AssetDatabase.FindAssets($"t:{typeof(BaseLevelSO)}");
            _levels = levelGuids
                .Select(g => AssetDatabase.LoadAssetAtPath<BaseLevelSO>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(l => l != null)
                .OrderBy(l => l.LevelID)
                .ToList();

        }

        public void UpdateLevelButtonText()
        {
            if (_isSceneOnly)
            {
                _levelDropdown.tooltip = "Select Level or Scene to play";

                if (_selectedScenePath == MainScenePath)
                {
                    _levelDropdown.SetText("Main Scene");
                }
                else if (_selectedScenePath == MainMenuPath)
                {
                    _levelDropdown.SetText("Main Menu");
                }
                else
                {
                    _levelDropdown.SetText("(Unknown Scene)");
                }

                _playButton.SetEnabled(true);
            }
            else if (_selectedLevel)
            {
                var levelName = _selectedLevel.name;
                var modeName = _selectedMode ? _selectedMode.DisplayName : "";
                var modeNameShorten = modeName.Length > 3 ? modeName.Substring(0, 3) + "…" : modeName;
                var fullDisplay = _selectedMode ? $"{modeName}/{levelName}" : levelName;
                var shortDisplay = _selectedMode ? $"{modeNameShorten}/{levelName}" : levelName;

                _levelDropdown.tooltip = $"{fullDisplay}. Select Level or Scene to play";

                var display = shortDisplay.Length > 15 ? shortDisplay.Substring(0, 14) + "…" : shortDisplay;
                _levelDropdown.SetText(display);

                _playButton.SetEnabled(true);
            }
            else
            {
                _levelDropdown.tooltip = "Tool disabled. Select Level or Scene to play";
                _levelDropdown.SetText("Select Level");
                _playButton.SetEnabled(false);
            }
        }

        private void PingAsset(Object asset)
        {
            if (!asset)
            {
                EditorUtility.DisplayDialog("Cannot Select", "The asset was not found.", "OK");
                return;
            }
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private void SaveSelection()
        {
            if (_isSceneOnly)
            {
                EditorPrefs.SetString(LevelPrefKey, _selectedScenePath);
                PlayLevelSelectionBridge.NotifyChanged();
                return;
            }

            if (_selectedLevel == null)
            {
                EditorPrefs.SetString(LevelPrefKey, DisabledFlag);
                PlayLevelSelectionBridge.NotifyChanged();
                return;
            }

            if (_selectedMode == null)
            {
                EditorPrefs.SetString(ModePrefKey, DisabledFlag);
            }
            else
            {
                var modePath = AssetDatabase.GetAssetPath(_selectedMode);
                EditorPrefs.SetString(ModePrefKey, modePath);
            }

            var path = AssetDatabase.GetAssetPath(_selectedLevel);
            EditorPrefs.SetString(LevelPrefKey, path);
            PlayLevelSelectionBridge.NotifyChanged();
        }

        public void RestoreSelection()
        {
            var levelPath = EditorPrefs.GetString(LevelPrefKey, "");

            if (string.IsNullOrEmpty(levelPath) || levelPath == DisabledFlag)
            {
                _selectedMode = null;
                _selectedLevel = null;
                _isSceneOnly = false;
                return;
            }

            if (levelPath == MainScenePath || levelPath == MainMenuPath)
            {
                _selectedLevel = null;
                _isSceneOnly = true;
                _selectedScenePath = levelPath;
                return;
            }

            var modePath = EditorPrefs.GetString(ModePrefKey, "");
            if (string.IsNullOrEmpty(modePath) || modePath == DisabledFlag)
            {
                _selectedMode = null;
            }
            else
            {
                _selectedMode = AssetDatabase.LoadAssetAtPath<GameModeSO>(modePath);
            }

            _selectedLevel = AssetDatabase.LoadAssetAtPath<BaseLevelSO>(levelPath);
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

            if (_selectedLevel == null)
            {
                PlayLevelHandler.SetSelectedLevel(_selectedLevel);
                EditorUtility.DisplayDialog("No Level Selected", "Please select a BaseLevelSO first.", "OK");
                return;
            }


            // Always start from MainScene
            var mainScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath);
            if (mainScene) EditorSceneManager.playModeStartScene = mainScene;

            // handoff to runtime bridge
            PlayLevelHandler.SetSelectedLevel(_selectedLevel);
            PlayLevelHandler.SetSelectedMode(_selectedMode);
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

            if (_selectedLevel == null) return;

            if (_selectedLevel.LevelCount() > 1)
            {
                EditorUtility.DisplayDialog("Cannot Open",
                    "This LevelSO contains multiple scenes and cannot be opened directly.", "OK");
                return;
            }

            if (_selectedLevel is LevelSO level)
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

            if (_selectedLevel)
            {
                Selection.activeObject = _selectedLevel;
                EditorGUIUtility.PingObject(_selectedLevel);
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
                //_openSceneButton.SetEnabled(false);
                //_selectButton.SetEnabled(false);
            }
            else
            {
                // Restore normal look
                //_openSceneButton.SetEnabled(true);
                //_selectButton.SetEnabled(true);
            }
        }
    }
}
