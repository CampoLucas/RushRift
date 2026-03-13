using UnityEngine;

namespace Game.Levels
{
    [System.Serializable]
    public class LevelSelectorData
    {
        public Color Color => color;
        
        [SerializeField] private Sprite previewImage;
        [SerializeField] private Color color;

        public bool TryGetPreview(out Sprite preview)
        {
            if (!previewImage)
            {
                preview = default;
                return false;
            }

            preview = previewImage;
            return true;
        }

        public Sprite GetPreviewOrDefault(Sprite defaultPreview)
        {
            return !TryGetPreview(out var preview) ? defaultPreview : preview;
        }
    }
}