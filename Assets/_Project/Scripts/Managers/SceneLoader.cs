using System.Collections;
using LudumDare.Template.Core;
using LudumDare.Template.Events;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LudumDare.Template.Managers
{
    /// <summary>
    /// Async scene loader with a UI-driven fade (LoadingScreen). Publishes void events on load
    /// start / completion so UI/gameplay can react.
    /// </summary>
    public class SceneLoader : Singleton<SceneLoader>
    {
        public const string BootstrapScene = "00_Bootstrap";
        public const string MainMenuScene  = "01_MainMenu";
        public const string GameScene      = "02_Game";

        [SerializeField] private VoidEventChannelSO _onLoadStart;
        [SerializeField] private VoidEventChannelSO _onLoadComplete;

        [Tooltip("Extra delay after scene activation so the loading fade reads as intentional.")]
        [SerializeField] private float _minLoadSeconds = 0.3f;

        public bool IsLoading { get; private set; }

        public void LoadMainMenu() => Load(MainMenuScene);
        public void LoadGame()     => Load(GameScene);

        public void Load(string sceneName)
        {
            if (IsLoading) return;
            StartCoroutine(LoadRoutine(sceneName));
        }

        public void RestartCurrentScene()
        {
            if (IsLoading) return;
            StartCoroutine(LoadRoutine(SceneManager.GetActiveScene().name));
        }

        private IEnumerator LoadRoutine(string sceneName)
        {
            IsLoading = true;
            _onLoadStart?.Raise();

            yield return null;

            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (op == null)
            {
                Debug.LogError($"[SceneLoader] Scene '{sceneName}' not found in Build Settings.");
                IsLoading = false;
                yield break;
            }

            op.allowSceneActivation = false;

            float started = Time.unscaledTime;
            while (op.progress < 0.9f)
            {
                yield return null;
            }

            float elapsed = Time.unscaledTime - started;
            if (elapsed < _minLoadSeconds)
            {
                yield return new WaitForSecondsRealtime(_minLoadSeconds - elapsed);
            }

            op.allowSceneActivation = true;
            while (!op.isDone) yield return null;

            IsLoading = false;
            ApplySceneSessionDefaults(sceneName);
            _onLoadComplete?.Raise();
        }

        /// <summary>
        /// Persistent managers (DDOL) keep score and pause across single-scene loads. When a gameplay
        /// scene is loaded, treat it as a fresh run; when returning to the menu, time must flow again.
        /// </summary>
        private static void ApplySceneSessionDefaults(string sceneName)
        {
            if (sceneName == GameScene)
            {
                if (GameManager.HasInstance)
                {
                    GameManager.Instance.StartNewGame();
                }

                return;
            }

            if (sceneName == MainMenuScene)
            {
                if (PauseService.HasInstance)
                {
                    PauseService.Instance.Resume();
                }

                if (PlayerSession.HasInstance)
                {
                    PlayerSession.Instance.ClearSession();
                }

                if (GameManager.HasInstance)
                {
                    GameManager.Instance.SetState(GameState.MainMenu);
                }
            }
        }
    }
}
