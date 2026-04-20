using TMPro;
using UnityEngine;

namespace LudumDare.Template.Gameplay.Signal
{
    /// <summary>
    /// Отображает снимок HUD через канал и TMP.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public sealed class SignalHudPresenter : MonoBehaviour
    {
        [SerializeField] private SignalGameController _controller;
        [SerializeField] private TMP_Text _nestChargeLabel;
        [SerializeField] private TMP_Text _nestHpLabel;
        [SerializeField] private TMP_Text _nestSatietyLabel;
        [SerializeField] private TMP_Text _nestLevelLabel;
        [SerializeField] private TMP_Text _waveLabel;
        [SerializeField] private TMP_Text _dashLabel;
        [SerializeField] private TMP_Text _trapSlowLabel;
        [SerializeField] private TMP_Text _trapAttractLabel;

        private SignalHudSnapshot _last;

        private void Awake()
        {
            if (_controller == null) _controller = FindAnyObjectByType<SignalGameController>();
        }

        private void OnEnable()
        {
            if (_controller == null) _controller = FindAnyObjectByType<SignalGameController>();
            if (_controller != null && _controller.HudChannel != null)
                _controller.HudChannel.OnHudUpdated += HandleHud;
            _controller?.RefreshHud();
        }

        private void OnDisable()
        {
            if (_controller != null && _controller.HudChannel != null)
                _controller.HudChannel.OnHudUpdated -= HandleHud;
        }

        private void HandleHud(SignalHudSnapshot s)
        {
            _last = s;
            if (_nestChargeLabel != null)
                _nestChargeLabel.text = $"Charge {s.NestCharge01 * 100f:0}%";
            if (_nestHpLabel != null)
                _nestHpLabel.text = $"Nest HP {s.NestHp01 * 100f:0}%";
            if (_nestSatietyLabel != null)
                _nestSatietyLabel.text = $"Satiety {s.NestSatiety01 * 100f:0}%";
            if (_nestLevelLabel != null)
                _nestLevelLabel.text = $"Nest Lv {s.NestLevel}";
            if (_waveLabel != null)
                _waveLabel.text = $"Wave {s.WaveDisplayIndex} · {s.EnemyCount} enemies";
            if (_dashLabel != null)
                _dashLabel.text = $"Dash {s.DashReady01 * 100f:0}%";
            if (_trapSlowLabel != null)
                _trapSlowLabel.text = $"Q slow {s.TrapSlowBar01 * 100f:0}%";
            if (_trapAttractLabel != null)
                _trapAttractLabel.text = $"E attract {s.TrapAttractBar01 * 100f:0}%";
        }
    }
}
