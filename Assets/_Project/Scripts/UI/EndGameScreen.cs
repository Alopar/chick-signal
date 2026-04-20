using LudumDare.Template.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LudumDare.Template.UI
{
    /// <summary>
    /// Итоговый оверлей: отдельные префабы для победы и поражения (разное оформление), одна логика кнопок и счёта.
    /// Показ инициирует <see cref="GameManager"/> через <see cref="PresentForState"/>.
    /// </summary>
    public sealed class EndGameScreen : UIScreen
    {
        public enum ResultKind
        {
            Victory = 0,
            Defeat = 1
        }

        [SerializeField] private ResultKind _resultKind = ResultKind.Defeat;
        [SerializeField] private TMP_Text _titleLabel;
        [SerializeField] private TMP_Text _scoreLabel;

        [Header("Buttons")]
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _leaderboardButton;
        [SerializeField] private Button _mainMenuButton;
        [Tooltip("Leaderboard screen (leave empty to build at runtime).")]
        [SerializeField] private UIScreen _leaderboardScreen;

        protected override void Awake()
        {
            base.Awake();
            if (_leaderboardScreen == null)
            {
                _leaderboardScreen = LeaderboardUiRuntimeBuilder.EnsureGameLeaderboard(transform);
            }

            EnsureRuntimeLeaderboardButton();

            if (_restartButton != null) _restartButton.onClick.AddListener(OnRestart);
            if (_leaderboardButton != null) _leaderboardButton.onClick.AddListener(OnLeaderboard);
            if (_mainMenuButton != null) _mainMenuButton.onClick.AddListener(OnMainMenu);
        }

        /// <summary>
        /// Показывает оверлей, соответствующий <paramref name="state"/> (победа или поражение).
        /// </summary>
        public static void PresentForState(GameState state)
        {
            if (state != GameState.GameOver && state != GameState.Victory) return;

            var want = state == GameState.Victory ? ResultKind.Victory : ResultKind.Defeat;
            var screens = FindObjectsByType<EndGameScreen>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < screens.Length; i++)
            {
                if (screens[i] == null || screens[i]._resultKind != want) continue;
                screens[i].Present();
                return;
            }
        }

        /// <summary>
        /// Обновляет счёт и выводит этот экземпер в стек UI.
        /// </summary>
        public void Present()
        {
            RefreshScoreLabel();

            if (UIManager.HasInstance)
                UIManager.Instance.Push(this, hideCurrent: false);
        }

        private void RefreshScoreLabel()
        {
            if (_scoreLabel == null || !GameManager.HasInstance) return;

            int best = SaveManager.HasInstance ? SaveManager.Instance.BestScore : GameManager.Instance.Score;
            _scoreLabel.text = $"Score: {GameManager.Instance.Score}\nBest: {best}";
        }

        private void OnRestart()
        {
            if (SceneLoader.HasInstance) SceneLoader.Instance.RestartCurrentScene();
        }

        private void OnLeaderboard()
        {
            if (_leaderboardScreen == null || !UIManager.HasInstance) return;
            UIManager.Instance.Push(_leaderboardScreen, hideCurrent: false);
        }

        private void EnsureRuntimeLeaderboardButton()
        {
            if (_leaderboardButton != null) return;
            var t = transform.Find("LeaderboardButton");
            if (t != null && t.TryGetComponent(out Button existing))
            {
                _leaderboardButton = existing;
                return;
            }

            _leaderboardButton = LeaderboardUiRuntimeBuilder.CreateMenuButton(
                transform,
                "LeaderboardButton",
                "Leaderboard",
                new Vector2(0.5f, 0.26f));
        }

        private void OnMainMenu()
        {
            if (SceneLoader.HasInstance) SceneLoader.Instance.LoadMainMenu();
        }
    }
}
