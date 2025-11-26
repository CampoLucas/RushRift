using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Game.UI.StateMachine.Elements
{
    public class PopUp : MonoBehaviour
    {
        [Header("View References")]
        [SerializeField] private TMP_Text titleText;

        [SerializeField] private TMP_Text infoText;
        [SerializeField] private Graphic background;
        [SerializeField] private Graphic iconImage;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Selectable defaultButton;
        [SerializeField] private Selectable backButton;
        
        [Header("Video")]
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private RawImage videoImage;
        
        [Header("Animation")]
        [SerializeField] private UIAnimation openAnim;
        [SerializeField] private UIAnimation closeAnim;
        [SerializeField] private float closeDelay;

        private bool _closed;
        private RenderTexture _instanceRenderTexture;

        private void Awake()
        {
            if (closeButton)
                closeButton.onClick.AddListener(CloseHandler);
            
            if (videoPlayer && videoImage)
            {
                var baseTexture = videoPlayer.targetTexture;
                if (baseTexture != null)
                {
                    _instanceRenderTexture = new RenderTexture(baseTexture);
                    _instanceRenderTexture.name = $"{baseTexture.name}_Instance_{GetInstanceID()}";
                    
                    videoPlayer.targetTexture = _instanceRenderTexture;

                    if (videoImage.texture == null || videoImage.texture == baseTexture)
                    {
                        videoImage.texture = _instanceRenderTexture;
                    }
                }
            }
        }


        public void Open(string title, string info, Color iconColor, Color backgroundColor, float delay = 0)
        {
            _closed = false;
            StopAllCoroutines();

            titleText.text = title;
            infoText.text = info;
            iconImage.color = iconColor;
            background.color = backgroundColor;

            SetVideo(null);

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
            EventSystem.current.SetSelectedGameObject(backButton.gameObject);
            if (confirmButton) confirmButton.onClick.RemoveAllListeners();
            
            StopAllCoroutines();
            StartCoroutine(CloseRoutine(closeDelay));
        }
        
        public void SetVideo(VideoClip clip)
        {
            if (!videoPlayer) return;

            if (clip == null)
            {
                videoPlayer.Stop();
                videoPlayer.clip = null;
                if (videoImage)
                {
                    videoImage.enabled = false;
                }
                return;
            }

            videoPlayer.clip = clip;
            if (videoImage)
            {
                videoImage.enabled = true;
            }
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

        public void ChangeContinueNav()
        {

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
            if (defaultButton != null)
            {
                EventSystem.current.SetSelectedGameObject(defaultButton.gameObject);
            }

            if (openAnim)
            {
                yield return openAnim.PlayRoutine(delay);
            }
            else
            {
                yield return null;
            }

            if (videoPlayer && videoPlayer.clip)
            {
                videoPlayer.Play();
            }
        }
        
        private IEnumerator CloseRoutine(float delay)
        {
            _closed = true;
            
            if (videoPlayer)
            {
                videoPlayer.Stop();
            }

            yield return closeAnim.PlayRoutine(delay);
            gameObject.SetActive(false);
        }
        
        private void OnDestroy()
        {
            if (closeButton) closeButton.onClick.RemoveAllListeners();
            if (confirmButton) confirmButton.onClick.RemoveAllListeners();

            if (_instanceRenderTexture != null)
            {
                _instanceRenderTexture.Release();
                Destroy(_instanceRenderTexture);
                _instanceRenderTexture = null;
            }

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
