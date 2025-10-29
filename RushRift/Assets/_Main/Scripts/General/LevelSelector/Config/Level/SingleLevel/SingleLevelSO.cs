using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Utils;
using MyTools.Global;
using Tools.Scripts.PropertyAttributes;
using UnityEditor;
using UnityEngine;

namespace Game.Levels.SingleLevel
{
    public abstract class SingleLevelSO : BaseLevelSO
    {
        public string SceneName => sceneName;
        public string ScenePath
        {
            get
            {
#if UNITY_EDITOR
                return AssetDatabase.GetAssetPath(scene);
#else
                return "";
#endif
            }
        }
        
        [Header("Scene")]
        [ReadOnly, SerializeField] private string sceneName;
#if UNITY_EDITOR
        [SerializeField] private UnityEditor.SceneAsset scene;
#endif
        
        public sealed override int LevelCount() => 1;

        public override SingleLevelSO GetLevel(int index)
        {
            if (index < 0 && index >= LevelCount())
            {
                this.Log("Level Index out of exception", LogType.Error);
                return null;
            }

            return this;
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (scene) sceneName = scene.name;
            else sceneName = SceneHandler.FirstLevel;
#endif
        }
    }
}