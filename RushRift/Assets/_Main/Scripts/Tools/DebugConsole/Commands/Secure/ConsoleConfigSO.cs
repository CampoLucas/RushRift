using System.Collections.Generic;
using Game.Levels;
using UnityEngine;

namespace Game.Tools.DebugCommands
{
    /// <summary>
    /// Password and game modes used by the debug console commands.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Debug Console Config", fileName = "ConsoleConfig")]
    public class ConsoleConfigSO : ScriptableObject
    {
        public string Password => password;
        public List<GameModeSO> GameModes => gameModes;

        [SerializeField] private string password;
        [Tooltip("Game modes load-gm can load. The id of each one is its asset name.")]
        [SerializeField] private List<GameModeSO> gameModes = new();
    }
}
