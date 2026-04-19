using LudumDare.Template.Core;
using LudumDare.Template.Events;
using UnityEngine;

namespace LudumDare.Template.Managers
{
    /// <summary>
    /// Drives <c>Time.timeScale</c> and broadcasts pause state. UI (PauseScreen) listens to the events here.
    /// </summary>
    public class PauseService : Singleton<PauseService>
    {
        [SerializeField] private VoidEventChannelSO _onPaused;
        [SerializeField] private VoidEventChannelSO _onResumed;

        public bool IsPaused { get; private set; }

        public void Pause()
        {
            if (IsPaused) return;
            IsPaused = true;
            Time.timeScale = 0f;
            _onPaused?.Raise();
        }

        public void Resume()
        {
            if (!IsPaused) return;
            IsPaused = false;
            Time.timeScale = 1f;
            _onResumed?.Raise();
        }

        public void Toggle()
        {
            if (IsPaused) Resume(); else Pause();
        }

        protected override void OnDestroy()
        {
            if (IsPaused) Time.timeScale = 1f;
            base.OnDestroy();
        }
    }
}
