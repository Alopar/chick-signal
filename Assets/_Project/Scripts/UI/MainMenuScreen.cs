using LudumDare.Template.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace LudumDare.Template.UI
{
    public class MainMenuScreen : UIScreen
    {
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _leaderboardButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private SettingsScreen _settingsScreen;
        [Tooltip("Nickname entry screen (leave empty to build at runtime).")]
        [SerializeField] private UIScreen _nicknameEntryScreen;
        [Tooltip("Leaderboard screen (leave empty to build at runtime).")]
        [SerializeField] private UIScreen _leaderboardScreen;

        protected override void Awake()
        {
            base.Awake();
            LeaderboardUiRuntimeBuilder.EnsureMainMenuScreens(transform);
            if (_nicknameEntryScreen == null)
            {
                _nicknameEntryScreen = LeaderboardUiRuntimeBuilder.FindChildScreen(transform.parent, "NicknameEntryScreen");
            }

            if (_leaderboardScreen == null)
            {
                _leaderboardScreen = LeaderboardUiRuntimeBuilder.FindChildScreen(transform.parent, "LeaderboardScreen");
            }

            EnsureRuntimeLeaderboardButton();

            if (_playButton != null) _playButton.onClick.AddListener(OnPlay);
            if (_leaderboardButton != null) _leaderboardButton.onClick.AddListener(OnLeaderboard);
            if (_settingsButton != null) _settingsButton.onClick.AddListener(OnSettings);
            if (_quitButton != null) _quitButton.onClick.AddListener(OnQuit);
        }

        private void OnPlay()
        {
            if (_nicknameEntryScreen != null && UIManager.HasInstance)
            {
                UIManager.Instance.Push(_nicknameEntryScreen);
                return;
            }

            if (SceneLoader.HasInstance) SceneLoader.Instance.LoadGame();
        }

        private void OnLeaderboard()
        {
            if (_leaderboardScreen != null && UIManager.HasInstance)
            {
                UIManager.Instance.Push(_leaderboardScreen);
            }
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
                new Vector2(0.5f, 0.46f));
        }

        private void OnSettings()
        {
            if (_settingsScreen != null && UIManager.HasInstance)
            {
                UIManager.Instance.Push(_settingsScreen);
            }
        }

        private void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
