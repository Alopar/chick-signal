using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LudumDare.Template.Input
{
    /// <summary>
    /// ScriptableObject facade over the <c>InputSystem_Actions</c> asset. Binds by name at runtime,
    /// so nothing depends on the generated C# wrapper class. Gameplay/UI subscribe to the C# events
    /// below.
    /// </summary>
    [CreateAssetMenu(menuName = "LudumDare/Input/Input Reader", fileName = "InputReader")]
    public class InputReader : ScriptableObject
    {
        [Header("Binding")]
        [SerializeField] private InputActionAsset _actions;
        [SerializeField] private string _playerMapName = "Player";
        [SerializeField] private string _uiMapName = "UI";

        public event Action<Vector2> OnMove;
        public event Action<Vector2> OnLook;
        public event Action OnAttack;
        public event Action OnAttackCancelled;
        public event Action OnInteract;
        public event Action OnJump;
        public event Action OnCrouch;
        public event Action OnCancel;
        public event Action OnCheatWin;
        public event Action OnCheatLose;

        private InputActionMap _player;
        private InputActionMap _ui;

        private InputAction _move, _look, _attack, _interact, _jump, _crouch, _cancel, _cheatWin, _cheatLose;

        public Vector2 MoveValue => _move != null ? _move.ReadValue<Vector2>() : Vector2.zero;
        public Vector2 LookValue => _look != null ? _look.ReadValue<Vector2>() : Vector2.zero;

        private void OnEnable() => Bind();
        private void OnDisable() => Unbind();

        private void Bind()
        {
            if (_actions == null) return;

            _player = _actions.FindActionMap(_playerMapName, throwIfNotFound: false);
            _ui = _actions.FindActionMap(_uiMapName, throwIfNotFound: false);

            _move     = _player?.FindAction("Move",     throwIfNotFound: false);
            _look     = _player?.FindAction("Look",     throwIfNotFound: false);
            _attack   = _player?.FindAction("Attack",   throwIfNotFound: false);
            _interact = _player?.FindAction("Interact", throwIfNotFound: false);
            _jump     = _player?.FindAction("Jump",     throwIfNotFound: false);
            _crouch   = _player?.FindAction("Crouch",   throwIfNotFound: false);
            _cheatWin = _player?.FindAction("CheatWin", throwIfNotFound: false);
            _cheatLose = _player?.FindAction("CheatLose", throwIfNotFound: false);
            _cancel   = _ui?.FindAction("Cancel",       throwIfNotFound: false);

            if (_move != null)     { _move.performed     += OnMovePerformed;     _move.canceled     += OnMovePerformed; }
            if (_look != null)     { _look.performed     += OnLookPerformed;     _look.canceled     += OnLookPerformed; }
            if (_attack != null)   { _attack.performed   += OnAttackPerformed;   _attack.canceled   += OnAttackCanceled; }
            if (_interact != null) { _interact.performed += OnInteractPerformed; }
            if (_jump != null)     { _jump.performed     += OnJumpPerformed; }
            if (_crouch != null)   { _crouch.performed   += OnCrouchPerformed; }
            if (_cheatWin != null) { _cheatWin.performed += OnCheatWinPerformed; }
            if (_cheatLose != null) { _cheatLose.performed += OnCheatLosePerformed; }
            if (_cancel != null)   { _cancel.performed   += OnCancelPerformed; }
        }

        private void Unbind()
        {
            if (_move != null)     { _move.performed     -= OnMovePerformed;     _move.canceled     -= OnMovePerformed; }
            if (_look != null)     { _look.performed     -= OnLookPerformed;     _look.canceled     -= OnLookPerformed; }
            if (_attack != null)   { _attack.performed   -= OnAttackPerformed;   _attack.canceled   -= OnAttackCanceled; }
            if (_interact != null) { _interact.performed -= OnInteractPerformed; }
            if (_jump != null)     { _jump.performed     -= OnJumpPerformed; }
            if (_crouch != null)   { _crouch.performed   -= OnCrouchPerformed; }
            if (_cheatWin != null) { _cheatWin.performed -= OnCheatWinPerformed; }
            if (_cheatLose != null) { _cheatLose.performed -= OnCheatLosePerformed; }
            if (_cancel != null)   { _cancel.performed   -= OnCancelPerformed; }

            DisableAll();
        }

        public void EnablePlayer()
        {
            if (_ui != null) _ui.Disable();
            if (_player != null) _player.Enable();
        }

        public void EnableUI()
        {
            if (_player != null) _player.Disable();
            if (_ui != null) _ui.Enable();
        }

        public void DisableAll()
        {
            if (_player != null) _player.Disable();
            if (_ui != null) _ui.Disable();
        }

        private void OnMovePerformed(InputAction.CallbackContext ctx)      => OnMove?.Invoke(ctx.ReadValue<Vector2>());
        private void OnLookPerformed(InputAction.CallbackContext ctx)      => OnLook?.Invoke(ctx.ReadValue<Vector2>());
        private void OnAttackPerformed(InputAction.CallbackContext ctx)    => OnAttack?.Invoke();
        private void OnAttackCanceled(InputAction.CallbackContext ctx)     => OnAttackCancelled?.Invoke();
        private void OnInteractPerformed(InputAction.CallbackContext ctx)  => OnInteract?.Invoke();
        private void OnJumpPerformed(InputAction.CallbackContext ctx)      => OnJump?.Invoke();
        private void OnCrouchPerformed(InputAction.CallbackContext ctx)    => OnCrouch?.Invoke();
        private void OnCheatWinPerformed(InputAction.CallbackContext ctx)  => OnCheatWin?.Invoke();
        private void OnCheatLosePerformed(InputAction.CallbackContext ctx) => OnCheatLose?.Invoke();
        private void OnCancelPerformed(InputAction.CallbackContext ctx)    => OnCancel?.Invoke();
    }
}
