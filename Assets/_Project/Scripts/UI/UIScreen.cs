using System.Collections;
using UnityEngine;

namespace LudumDare.Template.UI
{
    /// <summary>
    /// Base class for every full-screen UI panel. Wraps a <see cref="CanvasGroup"/> fade so screens can
    /// be pushed/popped by <see cref="UIManager"/> without each panel implementing its own transition.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIScreen : MonoBehaviour
    {
        [SerializeField] private float _fadeDuration = 0.2f;
        [SerializeField] private bool _blockRaycastsWhenHidden = false;

        [Header("Auto-show")]
        [Tooltip("If enabled, the screen shows on Start() via UIManager without an external Push.")]
        [SerializeField] private bool _showOnStart = false;
        [Tooltip("If enabled, auto-show clears the UI stack (Replace) instead of Push.")]
        [SerializeField] private bool _replacesStack = true;

        private CanvasGroup _canvasGroup;
        private Coroutine _fadeRoutine;

        public bool IsVisible { get; private set; }

        protected virtual void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = _blockRaycastsWhenHidden;
        }

        protected virtual void OnEnable()
        {
            if (UIManager.HasInstance) UIManager.Instance.Register(this);
        }

        protected virtual void OnDisable()
        {
            if (UIManager.HasInstance) UIManager.Instance.Unregister(this);
        }

        protected virtual void OnDestroy()
        {
            if (UIManager.HasInstance) UIManager.Instance.Unregister(this);
        }

        protected virtual void Start()
        {
            if (!_showOnStart) return;
            if (!UIManager.HasInstance) return;

            if (_replacesStack) UIManager.Instance.Replace(this);
            else                UIManager.Instance.Push(this);
        }

        public virtual void Show()
        {
            if (IsVisible) return;
            IsVisible = true;
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            OnShow();
            StartFade(1f, true);
        }

        public virtual void Hide()
        {
            if (!IsVisible) return;
            IsVisible = false;
            OnHide();
            StartFade(0f, false);
        }

        protected virtual void OnShow() { }
        protected virtual void OnHide() { }

        private void StartFade(float target, bool interactable)
        {
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(FadeRoutine(target, interactable));
        }

        private IEnumerator FadeRoutine(float target, bool interactable)
        {
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = true;

            float start = _canvasGroup.alpha;
            float t = 0f;
            float dur = Mathf.Max(0.0001f, _fadeDuration);

            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Lerp(start, target, t / dur);
                yield return null;
            }

            _canvasGroup.alpha = target;
            _canvasGroup.interactable = interactable;
            _canvasGroup.blocksRaycasts = interactable || _blockRaycastsWhenHidden;

            _fadeRoutine = null;
        }
    }
}
