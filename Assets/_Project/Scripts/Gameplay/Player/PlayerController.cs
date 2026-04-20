using LudumDare.Template.Input;
using LudumDare.Template.Managers;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LudumDare.Template.Gameplay.Player
{
    /// <summary>
    /// Minimal 2D player movement controller for template bootstrap.
    /// Reads Move from InputReader and applies velocity on X/Y axes.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        private static readonly int VisualStateHash = Animator.StringToHash("VisualState");

        [Header("References")]
        [SerializeField] private InputReader _inputReader;
        [SerializeField] private Rigidbody2D _rigidbody2D;
        [SerializeField] private Animator _animator;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private AudioCueSO _attackCue;

        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _movingSpeedThreshold = 0.05f;

        private Vector2 _moveInput;
        private Camera _mainCamera;

        private void Reset()
        {
            _rigidbody2D = GetComponent<Rigidbody2D>();
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
            if (_spriteRenderer == null) _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void Awake()
        {
            if (_rigidbody2D == null)
            {
                _rigidbody2D = GetComponent<Rigidbody2D>();
            }

            if (_animator == null) _animator = GetComponentInChildren<Animator>();
            if (_spriteRenderer == null) _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            _mainCamera = Camera.main;
        }

        private void OnEnable()
        {
            if (_inputReader == null) return;
            _inputReader.OnMove += HandleMove;
            _inputReader.OnAttack += HandleAttack;
            _inputReader.OnCheatWin += HandleCheatWin;
            _inputReader.OnCheatLose += HandleCheatLose;
            _inputReader.EnablePlayer();
        }

        private void OnDisable()
        {
            if (_inputReader == null) return;
            _inputReader.OnMove -= HandleMove;
            _inputReader.OnAttack -= HandleAttack;
            _inputReader.OnCheatWin -= HandleCheatWin;
            _inputReader.OnCheatLose -= HandleCheatLose;
        }

        private void FixedUpdate()
        {
            if (_rigidbody2D == null) return;
            _rigidbody2D.linearVelocity = _moveInput * _moveSpeed;
        }

        private void LateUpdate()
        {
            if (_rigidbody2D == null) return;

            float sq = _movingSpeedThreshold * _movingSpeedThreshold;
            bool moving = _rigidbody2D.linearVelocity.sqrMagnitude > sq;
            if (_animator != null && _inputReader != null)
            {
                int state = BertVisualState.Compute(
                    moving,
                    _inputReader.IsAttackHeld,
                    _inputReader.IsRepelHeld);
                _animator.SetInteger(VisualStateHash, state);
            }

            if (_spriteRenderer == null) return;

            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera == null) return;

            // Input System (в Player Settings включён только новый ввод — UnityEngine.Input недоступен).
            Vector2 screenPos;
            if (Pointer.current != null)
                screenPos = Pointer.current.position.ReadValue();
            else if (Mouse.current != null)
                screenPos = Mouse.current.position.ReadValue();
            else
                return;

            Vector3 mp = new Vector3(screenPos.x, screenPos.y, -_mainCamera.transform.position.z);
            float cursorWorldX = _mainCamera.ScreenToWorldPoint(mp).x;
            _spriteRenderer.flipX = cursorWorldX > transform.position.x;
        }

        private void HandleMove(Vector2 moveValue)
        {
            _moveInput = moveValue;
        }

        private void HandleAttack()
        {
            if (!CanProcessGameplayInput()) return;

            // Template example: +1 score per attack press for the run (reset in GameManager.StartNewGame).
            if (GameManager.HasInstance)
            {
                GameManager.Instance.AddScore(1);
            }

            if (_attackCue == null || !AudioManager.HasInstance) return;
            AudioManager.Instance.PlaySFX(_attackCue);
        }

        private void HandleCheatWin()
        {
            if (GameManager.HasInstance)
            {
                GameManager.Instance.Victory();
            }
        }

        private void HandleCheatLose()
        {
            if (GameManager.HasInstance)
            {
                GameManager.Instance.GameOver();
            }
        }

        private static bool CanProcessGameplayInput()
        {
            if (PauseService.HasInstance && PauseService.Instance.IsPaused) return false;
            return true;
        }
    }

    /// <summary>
    /// Соответствует параметру <c>VisualState</c> в <c>BertVisual.controller</c>.
    /// Контрсигнал имеет приоритет над сигналом, если зажаты оба.
    /// </summary>
    internal static class BertVisualState
    {
        public const int Idle = 0;
        public const int Run = 1;
        public const int SignalStand = 2;
        public const int SignalRun = 3;
        public const int CounterStand = 4;
        public const int CounterRun = 5;

        public static int Compute(bool moving, bool signalEmitting, bool counterSignal)
        {
            if (counterSignal) return moving ? CounterRun : CounterStand;
            if (signalEmitting) return moving ? SignalRun : SignalStand;
            return moving ? Run : Idle;
        }
    }
}
