using System;
using System.Collections;
using System.Collections.Generic;
using Game.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game.UI.Screens.Elements
{
    public class PopUp : MonoBehaviour
    {
        [Header("View References")]
        [SerializeField] private TMP_Text titleText;

        [SerializeField] private TMP_Text infoText;
        [SerializeField] private Graphic background;
        [SerializeField] private Image iconImage;
        [SerializeField] private Button closeButton;

        [Header("Animation")]
        [SerializeField] private UIAnimation openAnim;

        [SerializeField] private UIAnimation closeAnim;
        [SerializeField] private float closeDelay;

        private bool _closed;

        private void Awake()
        {
            closeButton.onClick.AddListener(CloseHandler);
        }

        private void CloseHandler()
        {
            if (_closed) return;
            _closed = true;
            closeButton.interactable = false;

            StopAllCoroutines();
            StartCoroutine(Close(closeDelay));
        }

        public void Open(string title, string info, Color iconColor, Color backgroundColor, float delay = 0)
        {
            StopAllCoroutines();
            StartCoroutine(OpenRoutine(title, info, iconColor, backgroundColor, delay));
        }

        public IEnumerator OpenRoutine(string title, string info, Color iconColor, Color backgroundColor, float delay = 0)
        {
            titleText.text = title;
            infoText.text = info;
            iconImage.color = iconColor;
            background.color = backgroundColor;

            gameObject.SetActive(true);

            if (openAnim)
            {
                yield return openAnim.PlayRoutine(delay);
            }
            else
            {
                yield return null;
            }
        }

        private IEnumerator Close(float delay)
        {
            _closed = true;

            yield return closeAnim.PlayRoutine(delay);

            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            closeButton.onClick.RemoveAllListeners();

            titleText = null;
            infoText = null;
            background = null;
            iconImage = null;
            closeButton = null;
            openAnim = null;
            closeAnim = null;
        }
    }
}
