using Game.DesignPatterns.Observers;
using UnityEngine;
using Game.Levels;
using MyTools.Global;

namespace Game
{
    [AddComponentMenu("Game/Level")]
    public class LevelHandler : MonoBehaviour
    {
        private Transform Spawn
        {
            get
            {
#if UNITY_EDITOR && DEBUG_SPAWN
                if (debugSpawn)
                {
                    this.Log("Using the debug spawn, remove the reference or remove the DEBUG_SPAWN symbol");
                    return debugSpawn;
                }
#endif
                return spawn;
            }
        }
        
        [Header("Level Data")]
        [SerializeField] private LevelSO levelConfig; // optional reference, useful for analytics or debugging.
        
        [Header("Gates")]
        [SerializeField] private Gate startGate;
        [SerializeField] private Gate endGate;
        [SerializeField] private WinTrigger goal;

        [Header("Spawn")]
        [SerializeField] private Transform spawn;
#if UNITY_EDITOR && DEBUG_SPAWN
        [Tooltip("A spawn that only works on editor, while it has a reference, it will ignore the regular spawn")]
        [SerializeField] private Transform debugSpawn;
#endif

        private NullCheck<LevelSO> _levelConfig;
        private ActionObserver _startTrigger;
        private ActionObserver _halfwayTrigger;
        private ActionObserver _endTrigger;

        // private bool _preloadedNext;
        // private bool _completed;
        // public LevelSO CurrentLevelSO => levelConfig;

        private void Awake()
        {
#if UNITY_EDITOR && LEVEL_CLEAR_OCCLUSION
            this.Log("Occlusion cleared, to disable this remove LEVEL_CLEAR_OCCLUSION defined symbol.");
            UnityEditor.StaticOcclusionCulling.Clear();
#endif
            _levelConfig = levelConfig;
            
#if false
            // this is just to test the scene in the editor
            if (!GlobalLevelManager.Instance && _levelConfig.TryGet(out var levelSo))
            {
                GameEntry.TryLoadLevelAsync(levelSo);
                return;
            }
#endif
            if (spawn && PlayerSpawner.Instance.TryGet(out var spawner))
            {
                spawner.SetSpawn(Spawn);
            }
            
            // _preloadedNext = false;
            // _completed = false;

            _startTrigger = new ActionObserver(OnStartHandler);
            _halfwayTrigger = new ActionObserver(OnHalfwayHandler);
            _endTrigger = new ActionObserver(OnEndHandler);
        }

        private void OnStartHandler()
        {
            
        }

        private void OnEndHandler()
        {
            
        }

        private void OnHalfwayHandler()
        {
            
        }
        
    }
}
