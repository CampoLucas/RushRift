using Game.Levels;
using MyTools.Global;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game.UI.StateMachine.Elements
{
    public sealed class LevelButton : MonoBehaviour, DesignPatterns.Observers.IObserver<ButtonSelectState>
    {
        [Header("Settings")]
        [SerializeField] private string lockedTitle = "???";
        
        [Header("References")]
        [SerializeField] private InteractiveButton button;
        [SerializeField] private Sprite defaultPreview;

        [Header("Visuals")]
        [SerializeField] private TMP_Text[] titles;
        [SerializeField] private Image coverImage;
        [SerializeField] private GameObject lockedVisual;
        [SerializeField] private Graphic[] graphics;

        [SerializeField] private Color defaultColor;
        [SerializeField] private SerializedDictionary<ButtonSelectState, Color> materials;
        
        
        private NullCheck<BaseLevelSO> _level;
        private NullCheck<UIAnimation> _runningAnim;

        private void Start()
        {
            button.Attach(this);
        }

        public void Init(BaseLevelSO level, bool unlocked)
        {
            _level = level;

            coverImage.sprite = level.LevelSelectData.GetPreviewOrDefault(defaultPreview);

            if (!unlocked || !_level)
            {
                Lock();
            }
            else
            {
                Unlock(_level.Get().LevelName);
            }
        }

        private void Lock()
        {
            button.enabled = false;
            lockedVisual.SetActive(true);
            SetTitle(lockedTitle);
        }

        private void Unlock(string displayName)
        {
            button.enabled = true;
            lockedVisual.SetActive(false);
            SetTitle(displayName);
        }

        private void SetTitle(string title)
        {
            for (var i = 0; i < titles.Length; i++)
            {
                titles[i].text = title;
            }
        }

        private void SetBorderColor(Color color)
        {
            //coverImage.color = color;
            
            for (var i = 0; i < graphics.Length; i++)
            {
                graphics[i].color = color;
            }
        }
        
        public void Select()
        {
            
        }
        
        public void Unselect()
        {
            
        }

        public void OnNotify(ButtonSelectState state)
        {
            if (materials.TryGetValue(state, out var c))
            {
                SetBorderColor(c);
            }
            else
            {
                SetBorderColor(defaultColor);
            }
        }

        public void Dispose()
        {
            if (button)
            {
                button.Detach(this);
            }

            button = null;
            materials.Dispose();
            materials = null;

            coverImage = null;

            titles = null;
            graphics = null;
        }
    }

}