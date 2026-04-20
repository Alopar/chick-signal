using System;
using System.Collections;
using UnityEngine;

namespace LudumDare.Template.Gameplay.Signal
{
    /// <summary>
    /// Проигрывает нарезанные кадры на <see cref="SpriteRenderer"/> без Animator (достаточно назначить спрайты).
    /// </summary>
    public sealed class BirdBornCutsceneSpritePlayer : MonoBehaviour
    {
        [SerializeField] private Sprite[] _frames;
        [SerializeField] private float _framesPerSecond = 12f;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [Tooltip("Если выключено — старт только через PlayFromStart() (например из SignalIntroCutsceneController).")]
        [SerializeField] private bool _playOnEnable = true;

        /// <summary>Срабатывает после показа последнего кадра.</summary>
        public event Action Finished;

        private Coroutine _running;

        private void OnEnable()
        {
            if (_playOnEnable && _frames != null && _frames.Length > 0)
                PlayFromStart();
        }

        private void OnDisable()
        {
            if (_running != null)
            {
                StopCoroutine(_running);
                _running = null;
            }
        }

        public void PlayFromStart()
        {
            if (!isActiveAndEnabled)
                return;
            if (_running != null)
                StopCoroutine(_running);
            _running = StartCoroutine(PlayRoutine());
        }

        public bool HasFrames => _frames != null && _frames.Length > 0;

        private IEnumerator PlayRoutine()
        {
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();

            if (_spriteRenderer == null || _frames == null || _frames.Length == 0)
            {
                Finished?.Invoke();
                _running = null;
                yield break;
            }

            float dt = 1f / Mathf.Max(0.01f, _framesPerSecond);
            for (int i = 0; i < _frames.Length; i++)
            {
                _spriteRenderer.sprite = _frames[i];
                yield return new WaitForSeconds(dt);
            }

            _running = null;
            Finished?.Invoke();
        }
    }
}
