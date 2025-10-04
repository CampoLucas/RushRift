using System;
using System.Collections;
using System.Collections.Generic;
using Game.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
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
        [SerializeField] private Button confirmButton;

        [Header("Animation")]
        [SerializeField] private UIAnimation openAnim;

        [SerializeField] private UIAnimation closeAnim;
        [SerializeField] private float closeDelay;

        private bool _closed;

        private void Awake()
        {
            closeButton.onClick.AddListener(CloseHandler);
        }

        public void Open(string title, string info, Color iconColor, Color backgroundColor, float delay = 0)
        {
            _closed = false;
            StopAllCoroutines();
            
            titleText.text = title;
            infoText.text = info;
            iconImage.color = iconColor;
            background.color = backgroundColor;
            
            StartCoroutine(OpenRoutine(delay));
        }

        public void Open(UnityAction onConfirm, float delay = 0)
        {
            _closed = false;
            StopAllCoroutines();
            
            if (confirmButton && onConfirm != null)
            {
                confirmButton.onClick.AddListener(onConfirm);
            }

            StartCoroutine(OpenRoutine(delay));
        }
        
        public void Close()
        {
            if (confirmButton) confirmButton.onClick.RemoveAllListeners();
            
            StopAllCoroutines();
            StartCoroutine(CloseRoutine(closeDelay));
        }

        public void Close(float delay)
        {
            StopAllCoroutines();
            StartCoroutine(CloseRoutine(delay));
        }
        
        private void CloseHandler()
        {
            if (_closed) return;
            _closed = true;
            closeButton.interactable = false;

            Close();
        }

        public IEnumerator OpenRoutine(string title, string info, Color iconColor, Color backgroundColor, float delay = 0)
        {
            _closed = false;
            titleText.text = title;
            infoText.text = info;
            iconImage.color = iconColor;
            background.color = backgroundColor;

            yield return OpenRoutine(delay);
        }
        public IEnumerator OpenRoutine(float delay = 0)
        {
            _closed = false;
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

        private IEnumerator CloseRoutine(float delay)
        {
            _closed = true;

            yield return closeAnim.PlayRoutine(delay);

            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (closeButton) closeButton.onClick.RemoveAllListeners();
            if (confirmButton) confirmButton.onClick.RemoveAllListeners();

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
