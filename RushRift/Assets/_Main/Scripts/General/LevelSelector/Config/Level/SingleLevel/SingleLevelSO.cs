using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Utils;
using Tools.Scripts.PropertyAttributes;
using UnityEngine;

namespace Game.Levels.SingleLevel
{
    public abstract class SingleLevelSO : BaseLevelSO
    {
        public string SceneName => sceneName;
        
        [Header("Scene")]
        [ReadOnly, SerializeField] private string sceneName;
#if UNITY_EDITOR
        [SerializeField] private UnityEditor.SceneAsset scene;
#endif
        
        public sealed override int LevelCount() => 1;
        
        private void OnValidate()
        {
#if UNITY_EDITOR
            if (scene) sceneName = scene.name;
            else sceneName = SceneHandler.FirstLevel;
#endif
        }
    }
}