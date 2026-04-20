using LudumDare.Template.Events;
using LudumDare.Template.Gameplay.Signal;
using LudumDare.Template.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LudumDare.Template.UI
{
    public class HUDScreen : UIScreen
    {
        [SerializeField] private IntEventChannelSO _scoreChannel;
        [SerializeField] private SignalInfoToastEventChannelSO _infoToastChannel;
        [SerializeField] private SignalEvolutionEventChannelSO _evolutionChannel;
        [SerializeField] private TMP_Text _scoreLabel;
        [SerializeField] private InfoToastLayerView _infoToastLayer;
        [SerializeField] private GameObject _evolutionCardsPanel;
        [SerializeField] private Button _healCardButton;
        [SerializeField] private Button _trapCardButton;
        [SerializeField] private Button _purgeCardButton;
        [SerializeField] private float _cardHoverLift = 14f;
        [SerializeField] private float _cardHoverDuration = 0.12f;

        private SignalGameController _signalController;
        private bool _isEvolutionPanelVisible;

        protected override void Awake()
        {
            base.Awake();
            if (_scoreChannel != null) _scoreChannel.OnEventRaised += HandleScore;
            if (_infoToastChannel != null) _infoToastChannel.OnToastRequested += HandleInfoToast;
            _signalController = FindAnyObjectByType<SignalGameController>();
            if (_signalController != null)
            {
                if (_infoToastChannel != null)
                    _signalController.SetInfoToastChannel(_infoToastChannel);
                if (_evolutionChannel == null)
                    _evolutionChannel = _signalController.EvolutionChannel;
            }

            if (_evolutionChannel != null)
                _evolutionChannel.OnVisibilityChanged += HandleEvolutionVisibility;

            AutoWireEvolutionCards();
            BindEvolutionButtons();
            SetupCardHover();
            SetEvolutionCardsVisible(false);
        }

        protected override void OnDestroy()
        {
            if (_scoreChannel != null) _scoreChannel.OnEventRaised -= HandleScore;
            if (_infoToastChannel != null) _infoToastChannel.OnToastRequested -= HandleInfoToast;
            if (_evolutionChannel != null) _evolutionChannel.OnVisibilityChanged -= HandleEvolutionVisibility;
            base.OnDestroy();
        }

        protected override void OnShow()
        {
            if (_scoreLabel != null && GameManager.HasInstance)
            {
                _scoreLabel.text = $"Score: {GameManager.Instance.Score}";
            }

            SetEvolutionCardsVisible(_isEvolutionPanelVisible);
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

        private void HandleEvolutionVisibility(bool visible)
        {
            _isEvolutionPanelVisible = visible;
            SetEvolutionCardsVisible(visible);
        }

        private void SetEvolutionCardsVisible(bool visible)
        {
            if (_evolutionCardsPanel != null)
                _evolutionCardsPanel.SetActive(visible);
        }

        private void AutoWireEvolutionCards()
        {
            if (_evolutionCardsPanel == null)
            {
                var panel = transform.Find("EvolutionCardsPanel");
                if (panel != null) _evolutionCardsPanel = panel.gameObject;
            }

            if (_healCardButton == null)
                _healCardButton = FindButton("EvolutionCardsPanel/CardHealButton");
            if (_trapCardButton == null)
                _trapCardButton = FindButton("EvolutionCardsPanel/CardTrapButton");
            if (_purgeCardButton == null)
                _purgeCardButton = FindButton("EvolutionCardsPanel/CardPurgeButton");
        }

        private Button FindButton(string path)
        {
            var child = transform.Find(path);
            if (child == null) return null;
            child.TryGetComponent(out Button button);
            return button;
        }

        private void BindEvolutionButtons()
        {
            BindButton(_healCardButton, ApplyHealBonus);
            BindButton(_trapCardButton, ApplyTrapBonus);
            BindButton(_purgeCardButton, ApplyPurgeBonus);
        }

        private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null) return;
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private void ApplyHealBonus() => ApplyBonus(SignalEvolutionBonus.Heal);
        private void ApplyTrapBonus() => ApplyBonus(SignalEvolutionBonus.Trap);
        private void ApplyPurgeBonus() => ApplyBonus(SignalEvolutionBonus.Purge);

        private void ApplyBonus(SignalEvolutionBonus bonus)
        {
            if (_signalController == null) _signalController = FindAnyObjectByType<SignalGameController>();
            _signalController?.ApplyEvolutionBonus(bonus);
        }

        private void SetupCardHover()
        {
            ConfigureCardHover(_healCardButton);
            ConfigureCardHover(_trapCardButton);
            ConfigureCardHover(_purgeCardButton);
        }

        private void ConfigureCardHover(Button button)
        {
            if (button == null) return;
            if (!button.TryGetComponent(out SignalCardHoverLift hover))
                hover = button.gameObject.AddComponent<SignalCardHoverLift>();
            hover.Configure(_cardHoverLift, _cardHoverDuration);
        }
    }
}
