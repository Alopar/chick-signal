using LudumDare.Template.Events;
using LudumDare.Template.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LudumDare.Template.UI
{
    /// <summary>
    /// Overlay shown on <see cref="GameState.GameOver"/> or <see cref="GameState.Victory"/>. Configure
    /// both labels in-prefab; the screen toggles which one is active based on the incoming state.
    /// </summary>
    public class EndGameScreen : UIScreen
    {
        [SerializeField] private GameStateEventChannelSO _stateChannel;
        [SerializeField] private TMP_Text _titleLabel;
        [SerializeField] private TMP_Text _scoreLabel;

        [Header("Buttons")]
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _leaderboardButton;
        [SerializeField] private Button _mainMenuButton;
        [Tooltip("Leaderboard screen (leave empty to build at runtime).")]
        [SerializeField] private UIScreen _leaderboardScreen;

        [Header("Copy")]
        [SerializeField] private string _victoryTitle = "VICTORY";
        [SerializeField] private string _gameOverTitle = "GAME OVER";

        protected override void Awake()
        {
            base.Awake();
            if (_leaderboardScreen == null)
            {
                _leaderboardScreen = LeaderboardUiRuntimeBuilder.EnsureGameLeaderboard(transform);
            }

            EnsureRuntimeLeaderboardButton();

            if (_stateChannel != null) _stateChannel.OnEventRaised += HandleState;
            if (_restartButton != null) _restartButton.onClick.AddListener(OnRestart);
            if (_leaderboardButton != null) _leaderboardButton.onClick.AddListener(OnLeaderboard);
            if (_mainMenuButton != null) _mainMenuButton.onClick.AddListener(OnMainMenu);
        }

        protected override void OnDestroy()
        {
            if (_stateChannel != null) _stateChannel.OnEventRaised -= HandleState;
            base.OnDestroy();
        }

        private void HandleState(GameState state)
        {
            if (state != GameState.GameOver && state != GameState.Victory) return;

            if (_titleLabel != null)
            {
                _titleLabel.text = state == GameState.Victory ? _victoryTitle : _gameOverTitle;
            }

            if (_scoreLabel != null && GameManager.HasInstance)
            {
                int best = SaveManager.HasInstance ? SaveManager.Instance.BestScore : GameManager.Instance.Score;
                _scoreLabel.text = $"Score: {GameManager.Instance.Score}\nBest: {best}";
            }

            if (UIManager.HasInstance) UIManager.Instance.Push(this, hideCurrent: false);
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
