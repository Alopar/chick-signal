using System.Collections;
using TMPro;
using UnityEngine;

namespace LudumDare.Template.UI
{
    public sealed class InfoToastItemView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private float _duration = 1.35f;
        [SerializeField] private float _fadeInDuration = 0.16f;
        [SerializeField] private float _fadeOutDuration = 0.2f;
        [SerializeField] private float _riseDistance = 36f;
        [SerializeField] private float _maxChars = 96f;

        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = transform as RectTransform;
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
        }

        public void SetupAndPlay(string text)
        {
            if (_label != null)
            {
                _label.text = string.IsNullOrWhiteSpace(text) ? string.Empty : text.Trim();
                _label.enableWordWrapping = true;
                _label.overflowMode = TextOverflowModes.Overflow;
                _label.maxVisibleCharacters = Mathf.RoundToInt(_maxChars);
            }

            StopAllCoroutines();
            StartCoroutine(PlayRoutine());
        }

        private IEnumerator PlayRoutine()
        {
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;
            Vector2 startPos = _rectTransform != null ? _rectTransform.anchoredPosition : Vector2.zero;
            Vector2 endPos = startPos + new Vector2(0f, _riseDistance);

            float t = 0f;
            while (t < _fadeInDuration)
            {
                t += Time.unscaledDeltaTime;
                float u = _fadeInDuration <= 0f ? 1f : Mathf.Clamp01(t / _fadeInDuration);
                if (_canvasGroup != null) _canvasGroup.alpha = u;
                yield return null;
            }

            float hold = Mathf.Max(0f, _duration - _fadeInDuration - _fadeOutDuration);
            if (hold > 0f) yield return new WaitForSecondsRealtime(hold);

            t = 0f;
            while (t < _fadeOutDuration)
            {
                t += Time.unscaledDeltaTime;
                float u = _fadeOutDuration <= 0f ? 1f : Mathf.Clamp01(t / _fadeOutDuration);
                if (_canvasGroup != null) _canvasGroup.alpha = 1f - u;
                if (_rectTransform != null) _rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, u);
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
