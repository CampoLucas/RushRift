using System.Collections;
using System.Collections.Generic;
using Game.Levels;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.UI.StateMachine.Elements
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

        private List<(PopUpData, MedalInfo)> _popUps = new();

        public void Init()
        {
            StopAllCoroutines();
            
            _popUps.Clear();
            bronze.popUp.gameObject.SetActive(false);
            silver.popUp.gameObject.SetActive(false);
            gold.popUp.gameObject.SetActive(false);
            var model = presenter.GetModel();
            
            _popUps.Clear();
            
            if (model.TryGetMedal(MedalType.Bronze, out var medalInfo) && medalInfo is { PrevUnlocked: false, Unlocked: true })
            {
                bronze.popUp.gameObject.SetActive(true);
                _popUps.Add((bronze, medalInfo));
            }
            
            if (model.TryGetMedal(MedalType.Bronze, out medalInfo) && medalInfo is { PrevUnlocked: false, Unlocked: true })
            {
                silver.popUp.gameObject.SetActive(true);
                _popUps.Add((silver, medalInfo));
            }
            
            if (model.TryGetMedal(MedalType.Bronze, out medalInfo) && medalInfo is { PrevUnlocked: false, Unlocked: true })
            {
                gold.popUp.gameObject.SetActive(true);
                _popUps.Add((gold, medalInfo));
            }
        }
        
        public void Play()
        {
            // var model = presenter.GetModel();
            //
            //
            //
            //
            // var bronzeInfo = model.BronzeInfo;
            // if (bronzeInfo is { PrevUnlocked: false, Unlocked: true })
            // {
            //     bronze.popUp.gameObject.SetActive(true);
            //     _popUps.Add((bronze, bronzeInfo));
            // }
            //
            // var silverInfo = model.SilverInfo;
            // if (silverInfo is { PrevUnlocked: false, Unlocked: true })
            // {
            //     silver.popUp.gameObject.SetActive(true);
            //     _popUps.Add((silver, silverInfo));
            // }
            //
            // var goldInfo = model.GoldInfo;
            // if (goldInfo is { PrevUnlocked: false, Unlocked: true })
            // {
            //     gold.popUp.gameObject.SetActive(true);
            //     _popUps.Add((gold, goldInfo));
            // }
            
            //Debug.LogError($"SuperTest: play popupgroup popUps: {_popUps.Count} {bronzeInfo.UpgradeName} {silverInfo.UpgradeName} {goldInfo.UpgradeName}");
            
            StartCoroutine(OpenPopUps());
        }

        private IEnumerator OpenPopUps()
        {
            var model = presenter.GetModel();

            for (var i = 0; i < _popUps.Count; i++)
            {
                var popUp = _popUps[i];

                yield return OpenMedal(popUp.Item1, popUp.Item2);
            }
            
            // var bronzeInfo = model.BronzeInfo;
            // if (bronzeInfo is { PrevUnlocked: false, Unlocked: true })
            // {
            //     yield return OpenMedal(bronze, bronzeInfo);
            // }
            //
            // var silverInfo = model.SilverInfo;
            // if (silverInfo is { PrevUnlocked: false, Unlocked: true })
            // {
            //     yield return OpenMedal(silver, silverInfo);
            // }
            //
            // var goldInfo = model.GoldInfo;
            // if (goldInfo is { PrevUnlocked: false, Unlocked: true })
            // {
            //     yield return OpenMedal(gold, goldInfo);
            // }
        }

        private IEnumerator OpenMedal(PopUpData data, MedalInfo medalInfo)
        {
            yield return data.popUp.OpenRoutine(title, medalInfo.UpgradeName, data.medalColor, data.backgroundColor, delay);
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
        public Color medalColor;
        [FormerlySerializedAs("color")] public Color backgroundColor;
        public PopUp popUp;
    }
}