using System.Collections;
using UnityEngine;

namespace Game.UI.Screens.Elements
{
    public class PopUpGroup : MonoBehaviour
    {
        [SerializeField] private LevelWonPresenter presenter;

        [Header("Settings")]
        [SerializeField] private string title;
        [SerializeField] private float delay = .1f;
        [SerializeField] private PopUpData bronze;
        [SerializeField] private PopUpData silver;
        [SerializeField] private PopUpData gold;

        public void Play()
        {
            StartCoroutine(OpenPopUps());
        }

        private IEnumerator OpenPopUps()
        {
            var model = presenter.GetModel();

            var bronzeInfo = model.BronzeInfo;
            if (bronzeInfo is { PrevUnlocked: false, Unlocked: true })
            {
                yield return OpenMedal(bronze, bronzeInfo);
            }
            
            var silverInfo = model.SilverInfo;
            if (silverInfo is { PrevUnlocked: false, Unlocked: true })
            {
                yield return OpenMedal(silver, silverInfo);
            }
            
            var goldInfo = model.GoldInfo;
            if (goldInfo is { PrevUnlocked: false, Unlocked: true })
            {
                yield return OpenMedal(gold, goldInfo);
            }
        }

        private IEnumerator OpenMedal(PopUpData data, MedalInfo medalInfo)
        {
            yield return data.popUp.OpenRoutine(title, medalInfo.UpgradeName, data.icon, data.color, delay);
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
        }
    }

    [System.Serializable]
    public struct PopUpData
    {
        public string name;
        public Sprite icon;
        public Color color;
        public PopUp popUp;
    }
}