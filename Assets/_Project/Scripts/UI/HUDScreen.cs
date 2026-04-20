using LudumDare.Template.Events;
using LudumDare.Template.Gameplay.Signal;
using LudumDare.Template.Managers;
using TMPro;
using UnityEngine;

namespace LudumDare.Template.UI
{
    public class HUDScreen : UIScreen
    {
        [SerializeField] private IntEventChannelSO _scoreChannel;
        [SerializeField] private SignalInfoToastEventChannelSO _infoToastChannel;
        [SerializeField] private TMP_Text _scoreLabel;
        [SerializeField] private InfoToastLayerView _infoToastLayer;

        protected override void Awake()
        {
            base.Awake();
            if (_scoreChannel != null) _scoreChannel.OnEventRaised += HandleScore;
            if (_infoToastChannel != null) _infoToastChannel.OnToastRequested += HandleInfoToast;
            if (_infoToastChannel != null)
            {
                var signalController = FindAnyObjectByType<SignalGameController>();
                if (signalController != null)
                    signalController.SetInfoToastChannel(_infoToastChannel);
            }
        }

        protected override void OnDestroy()
        {
            if (_scoreChannel != null) _scoreChannel.OnEventRaised -= HandleScore;
            if (_infoToastChannel != null) _infoToastChannel.OnToastRequested -= HandleInfoToast;
            base.OnDestroy();
        }

        protected override void OnShow()
        {
            if (_scoreLabel != null && GameManager.HasInstance)
            {
                _scoreLabel.text = $"Score: {GameManager.Instance.Score}";
            }
        }

        private void HandleScore(int value)
        {
            if (_scoreLabel != null) _scoreLabel.text = $"Score: {value}";
        }

        private void HandleInfoToast(SignalInfoToastEvent toastEvent)
        {
            if (_infoToastLayer == null) return;

            string message = toastEvent.Type switch
            {
                SignalInfoToastType.WaveStarted => $"Wave {Mathf.Max(1, toastEvent.WaveNumber)}",
                SignalInfoToastType.ComboReached => $"Combo x{Mathf.Max(1, toastEvent.ComboMultiplier)}",
                SignalInfoToastType.WaveScoreBonus => $"+{Mathf.Max(0, toastEvent.ScoreAmount)} score for wave {Mathf.Max(1, toastEvent.WaveNumber)}",
                _ => string.Empty
            };

            if (!string.IsNullOrWhiteSpace(message))
                _infoToastLayer.Show(message);
        }
    }
}
