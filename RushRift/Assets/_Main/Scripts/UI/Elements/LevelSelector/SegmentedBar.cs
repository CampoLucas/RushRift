using System;
using Game.Levels;
using Game.Saves;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Game.UI.Elements.LevelSelector
{
    public class SegmentedBar : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button leftArrow;
        [SerializeField] private Button rightArrow;
        [SerializeField] private Graphic[] segments;
        
        [Header("Materials")]
        [SerializeField] private Material blockedMaterial;
        [SerializeField] private Material selectedMaterial;
        [SerializeField] private Material unselectedMaterial;
        
        public Action<int> onValueChanged;
        
        private bool[] _unlocked;
        private int _current;
        
        private void Start()
        {
            leftArrow.onClick.AddListener(OnLeft);
            rightArrow.onClick.AddListener(OnRight);
        }
        
        public void Init(int levelId)
        {
            var save = SaveSystem.LoadGame();
            Setup(new []
            {
                save.IsMedalUnlocked(levelId, MedalType.Bronze),
                save.IsMedalUnlocked(levelId, MedalType.Silver),
                save.IsMedalUnlocked(levelId, MedalType.Gold)
            }, save.GetLevelMedalSelection(levelId));
        }

        public void Setup(bool[] unlocked, int selectedIndex)
        {
            _unlocked = unlocked;

            Debug.Log(selectedIndex);
            _current = selectedIndex;
            RefreshVisuals();
        }
        
        private void OnLeft()
        {
            if (_current <= 0) return;
            SetValue(_current - 1);
        }

        private void OnRight()
        {
            var next = _current + 1;
            if (next > segments.Length) return;
            if (!_unlocked[next - 1]) return; // next segment is locked
            SetValue(next);
        }

        private void SetValue(int index)
        {
            _current = index;
            RefreshVisuals();
            onValueChanged?.Invoke(_current);
        }

        private void RefreshVisuals()
        {
            for (var i = 0; i < segments.Length; i++)
            {
                if (!_unlocked[i])
                    segments[i].material = blockedMaterial;
                else if (i < _current) 
                    segments[i].material = selectedMaterial;
                else
                    segments[i].material = unselectedMaterial;
            }

            leftArrow.gameObject.SetActive(_current > 0 && _current <= segments.Length);

            // can go right if next segment exists and is unlocked
            var canGoRight = _current < segments.Length && _unlocked[_current];
            rightArrow.gameObject.SetActive(canGoRight);
        }

        private void OnDestroy()
        {
            leftArrow.onClick.RemoveListener(OnLeft);
            rightArrow.onClick.RemoveListener(OnRight);
        }
    }
}