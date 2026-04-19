using TMPro;
using UnityEngine;

namespace LudumDare.Template.Gameplay.Signal
{
    /// <summary>
    /// Отображает снимок HUD через канал (TMP при наличии, иначе OnGUI).
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

        private void OnGUI()
        {
            if (_nestChargeLabel != null) return;
            var s = _last;
            const float line = 22f;
            float y = 8f;
            GUI.Label(new Rect(8f, y, 500f, line), $"SIGNAL  Charge {s.NestCharge01 * 100f:0}%  HP {s.NestHp01 * 100f:0}%  Satiety {s.NestSatiety01 * 100f:0}%  Lv{s.NestLevel}");
            y += line;
            GUI.Label(new Rect(8f, y, 500f, line), $"Wave {s.WaveDisplayIndex}  Enemies {s.EnemyCount}  Dash {s.DashReady01 * 100f:0}%  Q {s.TrapSlowBar01 * 100f:0}%  E {s.TrapAttractBar01 * 100f:0}%");
        }
    }
}
