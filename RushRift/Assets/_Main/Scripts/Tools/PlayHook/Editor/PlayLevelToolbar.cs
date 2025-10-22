using System;
using System.Collections.Generic;
using System.Linq;
using Game.Levels;
using Game.Levels.SingleLevel;
using Game.Utils;
using Tools.PlayHook.Elements;
using Tools.PlayHook.Elements.Menu;
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

        public readonly EditorToolbarDropdown _levelDropdown;
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

        private static List<EditorAction> _optionsActions = new();

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
            _levelDropdown = new EditorToolbarDropdown()
            {
                name = "level-dropdown"
            };
            _levelDropdown.text = "(None)";
            _levelDropdown.tooltip = "Select Level or Scene to play";
            _levelDropdown.clicked += ShowLevelMenu;
            _levelDropdown.style.flexGrow = 0;
            _levelDropdown.style.flexShrink = 0;
            _levelDropdown.style.width = 125;
            _levelDropdown.style.height = 20;
            _levelDropdown.style.marginRight = 6;
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
            _playButton.style.marginRight = 5;
            Add(_playButton);
            
            // More options button
            _moreOptions = new OptionsButton("…", isToolbar)
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
                    entries.Add(new MenuItem("Open Scene", () => OpenScene(_selectedScenePath), false, EnabledEntry));
                }
                else
                {
                    entries.Add(new MenuItem($"Open {_selectedLevel.LevelName} Scene", () => OpenLevelScene(_selectedLevel.GetLevel(0)), false, EnabledEntry));
                }
                return true;
            }

            var count = _selectedLevel.LevelCount();
            var scenes = new List<MenuEntry>();
            
            for (var i = 0; i < count; i++)
            {
                var level = _selectedLevel.GetLevel(i);

                if (level.IsNullOrMissingReference())
                {
                    entries.Add(new MenuItem("Null level", null, false, DisabledEntry));
                    continue;
                }
                
                scenes.Add(new MenuItem($"Open {level.LevelName} scene", () => OpenLevelScene(level), false, EnabledEntry));
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

#if false
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
                var selected = !_isSceneOnly && _selectedMode == null && _selectedLevel == level;
                
                menu.AddItem(new GUIContent(label), selected, () =>
                {
                    _selectedMode = null;
                    _selectedLevel = level;
                    _isSceneOnly = false;
                    SaveSelection();
                    UpdateLevelButtonText();
                });
            }
        }
#else
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
#endif

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
                    _levelDropdown.text = "Main Scene";
                else if (_selectedScenePath == MainMenuPath)
                    _levelDropdown.text = "Main Menu";
                else
                    _levelDropdown.text = "(Unknown Scene)";

                _playButton.SetEnabled(true);
                //_openSceneButton.SetEnabled(true);
                //_selectDropdown.SetEnabled(true);
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
                _levelDropdown.text = display;

                _playButton.SetEnabled(true);

                var multi = _selectedLevel is CompositeLevelSO;
                //_openSceneButton.SetEnabled(!multi);
                //_selectDropdown.SetEnabled(true);
            }
            else
            {
                _levelDropdown.tooltip = "Tool disabled. Select Level or Scene to play";
                _levelDropdown.text = "Select Level";
                _playButton.SetEnabled(false);
                //_openSceneButton.SetEnabled(false);
                //_selectDropdown.SetEnabled(false);
            }

            UpdateSelectButton();
        }

        private void UpdateSelectButton()
        {
            //var actions = BuildSelectActions();
        
            // _selectDropdown.tooltip = actions.Count switch
            // {
            //     0 => "No selectable assets for this item",
            //     1 => actions[0].Tooltip,
            //     _ => "Select an asset"
            // };
            //
            // _selectDropdown.SetEnabled(actions.Count > 0);
            // SetButtonVisual(_selectDropdown, actions.Count == 1);
        }
        
        private void SetButtonVisual(EditorButtonDropdown dropdown, bool asButton)
        {
            // Reset any overrides
            dropdown.style.backgroundImage = null;
            dropdown.style.unityBackgroundImageTintColor = Color.clear;

            if (asButton)
            {
                // Hide the ▼ arrow
                var element = dropdown.Arrow;
                if (element != null) element.style.display = DisplayStyle.None;

                // Make it look like a regular button
                dropdown.style.unityBackgroundImageTintColor = new Color(0.2f, 0.6f, 1f, 0.4f);
                dropdown.style.borderTopLeftRadius = 4;
                dropdown.style.borderTopRightRadius = 4;
                dropdown.style.borderBottomLeftRadius = 4;
                dropdown.style.borderBottomRightRadius = 4;
            }
            else
            {
                // Show arrow again for dropdown mode
                var element = dropdown.Arrow;
                if (element != null) element.style.display = DisplayStyle.Flex;
                dropdown.style.unityBackgroundImageTintColor = Color.clear;
            }
        }
        
        private List<EditorAction> BuildSelectActions()
        {
            var list = new List<EditorAction>();

            if (_isSceneOnly)
            {
                var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(_selectedScenePath);
                if (scene != null)
                    list.Add(new("Select Scene Asset", "Ping scene asset in Project", () => PingAsset(scene)));
                return list;
            }

            if (_selectedLevel == null)
                return list;

            // Try add scene (if it exists)
            if (_selectedLevel is SingleLevelSO lvl)
            {
                var scenePath = $"Assets/_Main/Scenes/Levels/{lvl.SceneName}.unity";
                var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
                if (scene != null)
                    list.Add(new("Select Level Scene", "Ping scene asset in Project", () => PingAsset(scene)));
            }

            // Add GameMode (if it exists)
            if (_selectedMode != null)
            {
                list.Add(new("Select Game Mode Asset", "Ping GameMode ScriptableObject", () => PingAsset(_selectedMode)));
            }
            
            // Always add Level asset
            list.Add(new("Select Level Asset", "Ping level ScriptableObject in Project", () => PingAsset(_selectedLevel)));

            return list;
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
                return;
            }

            if (_selectedLevel == null)
            {
                EditorPrefs.SetString(LevelPrefKey, DisabledFlag);
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
