using LudumDare.Template.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace LudumDare.Template.UI
{
    public class PauseScreen : UIScreen
    {
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _mainMenuButton;

        protected override void Awake()
        {
            base.Awake();
            if (_resumeButton != null)   _resumeButton.onClick.AddListener(OnResume);
            if (_restartButton != null)  _restartButton.onClick.AddListener(OnRestart);
            if (_mainMenuButton != null) _mainMenuButton.onClick.AddListener(OnMainMenu);
        }

        private void OnResume()
        {
            if (PauseService.HasInstance) PauseService.Instance.Resume();
            if (UIManager.HasInstance) UIManager.Instance.Pop();
        }

        private void OnRestart()
        {
            if (PauseService.HasInstance) PauseService.Instance.Resume();
            if (SceneLoader.HasInstance) SceneLoader.Instance.RestartCurrentScene();
        }

        private void OnMainMenu()
        {
            if (PauseService.HasInstance) PauseService.Instance.Resume();
            if (SceneLoader.HasInstance) SceneLoader.Instance.LoadMainMenu();
        }
    }
}
