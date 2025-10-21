using System.Collections.Generic;
using System.Linq;
using Game.Levels;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

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

        private EditorToolbarDropdown _levelDropdown;
        private EditorToolbarButton _playButton;
        private EditorToolbarButton _openSceneButton;
        private EditorToolbarButton _selectButton;

        private static List<GameModeSO> _gameModes = new();
        private static List<BaseLevelSO> _levels = new();
        private static BaseLevelSO _selectedLevel;
        private static GameModeSO _selectedMode;
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

            RefreshAssets();
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

        private void UpdateLevelButtonText()
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
                _openSceneButton.SetEnabled(true);
                _selectButton.SetEnabled(true);
                return;
            }

            if (_selectedLevel)
            {
                var n = _selectedLevel.name;
                var fullDisplay = _selectedMode ? $"{_selectedMode.DisplayName}/{n}" : n;

                _levelDropdown.tooltip = $"{fullDisplay}. Select Level or Scene to play";

                var display = fullDisplay.Length > 10 ? fullDisplay.Substring(0, 9) + "…" : fullDisplay;
                _levelDropdown.text = display;

                _playButton.SetEnabled(true);

                var multi = _selectedLevel is CompositeLevelSO;
                _openSceneButton.SetEnabled(!multi);
                _selectButton.SetEnabled(true);
            }
            else
            {
                _levelDropdown.tooltip = "Tool disabled. Select Level or Scene to play";
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
        }

        private void RestoreSelection()
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
}
