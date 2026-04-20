using System;
using TMPro;
using UnityEngine;

namespace LudumDare.Template.Gameplay.Signal
{
    [RequireComponent(typeof(TextMeshPro))]
    public sealed class SignalFloatingPopupView : MonoBehaviour
    {
        [SerializeField] private TextMeshPro _label;
        [SerializeField] private float _duration = 0.8f;
        [SerializeField] private float _riseDistance = 0.85f;
        [SerializeField] private float _driftX = 0.2f;
        [SerializeField] private AnimationCurve _alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        private Action<SignalFloatingPopupView> _onFinished;
        private Color _baseColor = Color.white;
        private Vector3 _startPos;
        private Vector3 _endPos;
        private float _elapsed;
        private bool _running;

        private void Awake()
        {
            EnsureLabel();
        }

        private void OnEnable()
        {
            EnsureLabel();
        }

        public void Play(string text, Color color, Vector3 position, Action<SignalFloatingPopupView> onFinished)
        {
            EnsureLabel();
            _onFinished = onFinished;
            _baseColor = color;
            _startPos = position;
            float drift = UnityEngine.Random.Range(-_driftX, _driftX);
            _endPos = position + new Vector3(drift, _riseDistance, 0f);
            _elapsed = 0f;
            _running = true;
            transform.position = _startPos;
            _label.text = text;
            _label.color = color;
        }

        private void Update()
        {
            if (!_running) return;

            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / Mathf.Max(1e-6f, _duration));
            transform.position = Vector3.LerpUnclamped(_startPos, _endPos, t);

            float alpha = Mathf.Clamp01(_alphaCurve.Evaluate(t));
            var c = _baseColor;
            c.a *= alpha;
            _label.color = c;

            if (t >= 1f)
            {
                _running = false;
                _onFinished?.Invoke(this);
            }
        }

        private void EnsureLabel()
        {
            if (_label != null) return;

            _label = GetComponent<TextMeshPro>();
            if (_label == null)
            {
                _label = gameObject.AddComponent<TextMeshPro>();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureLabel();
        }
#endif
    }
}
