using UnityEngine;
using UnityEngine.EventSystems;

namespace LudumDare.Template.UI
{
    public sealed class SignalCardHoverLift : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private RectTransform _target;
        [SerializeField] private float _hoverScaleMultiplier = 1.035f;
        [SerializeField] private float _moveDuration = 0.12f;
        [SerializeField] private bool _useUnscaledTime = true;

        private Vector3 _restScale;
        private Vector3 _targetScale;
        private bool _isHovered;

        public void Configure(float liftDistance, float moveDuration)
        {
            // Keep signature for HUD compatibility, but no position offsets are used.
            _hoverScaleMultiplier = liftDistance > 0f ? 1.035f : 1f;
            _moveDuration = Mathf.Max(0.01f, moveDuration);
        }

        private void Awake()
        {
            if (_target == null)
                _target = transform as RectTransform;
            if (_target != null)
            {
                _restScale = _target.localScale;
                _targetScale = _restScale;
            }
        }

        private void OnEnable()
        {
            if (_target == null)
                _target = transform as RectTransform;
            if (_target == null) return;

            _restScale = _target.localScale;
            _targetScale = _restScale;
            _isHovered = false;
        }

        private void OnDisable()
        {
            if (_target != null)
                _target.localScale = _restScale;
            _isHovered = false;
        }

        private void Update()
        {
            if (_target == null) return;

            float dt = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(dt / Mathf.Max(0.01f, _moveDuration));
            Vector3 desired = _isHovered ? _restScale * _hoverScaleMultiplier : _restScale;
            _targetScale = Vector3.Lerp(_targetScale, desired, t);
            _target.localScale = _targetScale;
        }

        public void OnPointerEnter(PointerEventData eventData) => _isHovered = true;
        public void OnPointerExit(PointerEventData eventData) => _isHovered = false;
    }
}
