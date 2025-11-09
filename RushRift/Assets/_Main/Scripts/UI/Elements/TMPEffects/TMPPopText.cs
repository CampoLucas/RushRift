using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;

namespace Game.UI.Elements
{
    [RequireComponent(typeof(TMP_Text))]
    public class TMPPopText : ObserverComponent
    {
        [Header("Settings")]
        [SerializeField] private bool startOnEnabled = false;
        [SerializeField] private float delayBetweenChars = 0.05f;
        [SerializeField] private float popDuration = 0.2f;
        [SerializeField] private AnimationCurve popCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private float popScale = 1.3f;

        private TMP_Text _tmp;
        private string _fullText;
        private bool _started;

        private void Awake()
        {
            _tmp = GetComponent<TMP_Text>();
            _fullText = _tmp.text;
        }

        private void OnEnable()
        {
            if (startOnEnabled)
            {
                StartEffect();
            }
            else
            {
                _tmp.text = "";
            }
        }

        private void StartEffect()
        {
            if (_started) return;
            _started = true;
            
            StopAllCoroutines();
            StartCoroutine(AnimateText());
        }

        private IEnumerator AnimateText()
        {
            var old = _tmp.text;
            _tmp.text = _fullText;
            _tmp.ForceMeshUpdate();
            var totalChars = _tmp.textInfo.characterCount;
            _tmp.text = string.Empty;

            if (totalChars == 0)
            {
                Debug.LogWarning($"{name}: TMPPopText – text has no visible characters");
                yield break;
            }

            var displayed = new StringBuilder();
            var visibleCount = 0;

            while (visibleCount < totalChars)
            {
                displayed.Clear();
                var charCount = 0;
                var insideTag = false;

                // Build visible substring while keeping tags intact
                for (var i = 0; i < _fullText.Length; i++)
                {
                    var c = _fullText[i];
                    if (c == '<') insideTag = true;
                    if (!insideTag) charCount++;
                    displayed.Append(c);
                    if (c == '>') insideTag = false;

                    if (charCount > visibleCount)
                        break;
                }

                _tmp.text = displayed.ToString();
                _tmp.ForceMeshUpdate();
                yield return StartCoroutine(PopCharacter(visibleCount));

                visibleCount++;
                yield return new WaitForSeconds(delayBetweenChars);
            }
            
            //_tmp.text = old;
        }

        private IEnumerator PopCharacter(int index)
        {
            _tmp.ForceMeshUpdate();
            var textInfo = _tmp.textInfo;
            if (index >= textInfo.characterCount) yield break;

            var charInfo = textInfo.characterInfo[index];
            if (!charInfo.isVisible) yield break;

            var meshIndex = charInfo.materialReferenceIndex;
            var vertexIndex = charInfo.vertexIndex;

            var vertices = textInfo.meshInfo[meshIndex].vertices;
            var original = new Vector3[4];
            for (var i = 0; i < 4; i++)
                original[i] = vertices[vertexIndex + i];

            var center = (original[0] + original[2]) / 2;
            var t = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime / popDuration;
                var scale = Mathf.Lerp(1f, popScale, popCurve.Evaluate(t));

                for (var j = 0; j < 4; j++)
                    vertices[vertexIndex + j] = (original[j] - center) * scale + center;

                _tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
                yield return null;
            }

            // Ensure it goes back to exact original scale
            for (var i = 0; i < 4; i++)
                vertices[vertexIndex + i] = original[i];

            _tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
        }

        public override void OnNotify(string arg)
        {
            StartEffect();
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
        }
    }
}