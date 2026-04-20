using LudumDare.Template.Managers;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LudumDare.Template.UI
{
    /// <summary>
    /// Toggles the <see cref="PauseScreen"/> on keyboard P / gamepad Start. Lives in the Game scene;
    /// <see cref="HUDScreen"/> can call <see cref="TogglePauseMenu"/> with the same behaviour.
    /// </summary>
    public class PauseInputWatcher : MonoBehaviour
    {
        [SerializeField] private PauseScreen _pauseScreen;

        /// <summary>Opens or closes the pause overlay (same logic as the keyboard shortcut).</summary>
        public static void TogglePauseMenu(PauseScreen pauseScreen)
        {
            PauseScreen screen = pauseScreen != null
                ? pauseScreen
                : UnityEngine.Object.FindAnyObjectByType<PauseScreen>();

            if (screen == null || !PauseService.HasInstance || !UIManager.HasInstance) return;

            if (PauseService.Instance.IsPaused)
            {
                PauseService.Instance.Resume();
                UIManager.Instance.Pop();
            }
            else
            {
                PauseService.Instance.Pause();
                UIManager.Instance.Push(screen, hideCurrent: false);
            }
        }

        private void Update()
        {
            if (Keyboard.current == null) return;
            if (!Keyboard.current.pKey.wasPressedThisFrame &&
                (Gamepad.current == null || !Gamepad.current.startButton.wasPressedThisFrame))
            {
                return;
            }

            TogglePauseMenu(_pauseScreen);
        }
    }
}
