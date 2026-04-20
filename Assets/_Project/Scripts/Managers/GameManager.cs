using LudumDare.Template.Core;
using LudumDare.Template.Events;
using LudumDare.Template.UI;
using UnityEngine;

namespace LudumDare.Template.Managers
{
    /// <summary>
    /// Tiny FSM around <see cref="GameState"/>. Gameplay systems mutate state through the public API;
    /// UI subscribes to <c>_stateChannel</c> to reflect the current state.
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        [SerializeField] private GameStateEventChannelSO _stateChannel;
        [SerializeField] private IntEventChannelSO _scoreChannel;

        public GameState State { get; private set; } = GameState.Boot;
        public int Score { get; private set; }

        private bool _scoreSubmittedThisRun;

        public void SetState(GameState next)
        {
            if (State == next) return;
            State = next;
            _stateChannel?.Raise(next);
        }

        public void StartNewGame()
        {
            if (PauseService.HasInstance)
            {
                PauseService.Instance.Resume();
            }

            _scoreSubmittedThisRun = false;
            Score = 0;
            _scoreChannel?.Raise(Score);
            SetState(GameState.Playing);
        }

        public void AddScore(int amount)
        {
            Score += amount;
            _scoreChannel?.Raise(Score);
        }

        public void GameOver()
        {
            if (PauseService.HasInstance)
            {
                PauseService.Instance.Pause();
            }

            if (SaveManager.HasInstance) SaveManager.Instance.TrySetBestScore(Score);
            TrySubmitScoreToLeaderboard();
            SetState(GameState.GameOver);
            EndGameScreen.PresentForState(GameState.GameOver);
        }

        public void Victory()
        {
            if (PauseService.HasInstance)
            {
                PauseService.Instance.Pause();
            }

            if (SaveManager.HasInstance) SaveManager.Instance.TrySetBestScore(Score);
            TrySubmitScoreToLeaderboard();
            SetState(GameState.Victory);
            EndGameScreen.PresentForState(GameState.Victory);
        }

        private void TrySubmitScoreToLeaderboard()
        {
            if (_scoreSubmittedThisRun) return;
            if (!PlayerSession.HasInstance || !PlayerSession.Instance.HasNickname) return;
            if (!LeaderboardClient.HasInstance) return;

            _scoreSubmittedThisRun = true;
            LeaderboardClient.Instance.SubmitScore(PlayerSession.Instance.CurrentNickname, Score);
        }
    }
}
