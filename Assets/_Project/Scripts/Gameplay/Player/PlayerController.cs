using LudumDare.Template.Input;
using LudumDare.Template.Managers;
using UnityEngine;

namespace LudumDare.Template.Gameplay.Player
{
    /// <summary>
    /// Minimal 2D player movement controller for template bootstrap.
    /// Reads Move from InputReader and applies velocity on X/Y axes.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InputReader _inputReader;
        [SerializeField] private Rigidbody2D _rigidbody2D;
        [SerializeField] private AudioCueSO _attackCue;

        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 5f;

        private Vector2 _moveInput;

        private void Reset()
        {
            _rigidbody2D = GetComponent<Rigidbody2D>();
        }

        private void Awake()
        {
            if (_rigidbody2D == null)
            {
                _rigidbody2D = GetComponent<Rigidbody2D>();
            }
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
}
