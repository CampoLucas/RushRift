using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.UI.Screens.Elements
{
    public class MedalsPopUps : MonoBehaviour
    {
        [SerializeField] private UIAnimation bronzePopUp;
        [SerializeField] private UIAnimation silverPopUp;
        [SerializeField] private UIAnimation goldPopUp;

        [SerializeField] private float popUpDelay;

        public void Play()
        {
            var data = SaveAndLoad.Load();
            var currentLevel = SceneManager.GetActiveScene().buildIndex;
            var medals = data.LevelsMedalsTimes[currentLevel];
            var time = LevelManager.LevelCompleteTime();

            var showBronze = false;
            var showSilver = false;
            var showGold = false;
            
            if (data.LevelsMedalsTimes[currentLevel].bronze.time > time && !medals.bronze.isAcquired)
            {
                medals.bronze.isAcquired = true;
                showBronze = true;
            }
            
            if (data.LevelsMedalsTimes[currentLevel].silver.time > time && !medals.silver.isAcquired)
            {
                medals.silver.isAcquired = true;
                showSilver = true;
            } 

            if (data.LevelsMedalsTimes[currentLevel].gold.time > time && !medals.gold.isAcquired)
            {
                medals.gold.isAcquired = true;
                showGold = true;
            }
            
            data.LevelsMedalsTimes[currentLevel] = medals;

            StartCoroutine(OpenPopUps(showBronze, showSilver, showGold));
        }

        private IEnumerator OpenPopUps(bool bronze, bool silver, bool gold)
        {
            if (bronze)
            {
                bronzePopUp.gameObject.SetActive(true);

                yield return bronzePopUp.PlayRoutine(popUpDelay);
            }
            
            if (silver)
            {
                silverPopUp.gameObject.SetActive(true);

                yield return silverPopUp.PlayRoutine(popUpDelay);
            }
            
            if (gold)
            {
                goldPopUp.gameObject.SetActive(true);

                yield return goldPopUp.PlayRoutine(popUpDelay);
            }
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
        }
    }
}