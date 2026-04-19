using LudumDare.Template.Managers;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LudumDare.Template.UI
{
    /// <summary>
    /// Toggles the <see cref="PauseScreen"/> on ESC / gamepad Start. Lives in the Game scene and
    /// grabs the screen reference through <see cref="UIManager"/>.
    /// </summary>
    public class PauseInputWatcher : MonoBehaviour
    {
        [SerializeField] private PauseScreen _pauseScreen;

        private void Update()
        {
            if (Keyboard.current == null) return;
            if (!Keyboard.current.escapeKey.wasPressedThisFrame &&
                (Gamepad.current == null || !Gamepad.current.startButton.wasPressedThisFrame))
            {
                return;
            }

            if (_pauseScreen == null || !PauseService.HasInstance || !UIManager.HasInstance) return;

            if (PauseService.Instance.IsPaused)
            {
                PauseService.Instance.Resume();
                UIManager.Instance.Pop();
            }
            else
            {
                PauseService.Instance.Pause();
                UIManager.Instance.Push(_pauseScreen, hideCurrent: false);
            }
        }
    }
}
