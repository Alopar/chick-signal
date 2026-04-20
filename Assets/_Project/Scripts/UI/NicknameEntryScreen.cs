using LudumDare.Template.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LudumDare.Template.UI
{
    public class NicknameEntryScreen : UIScreen
    {
        [SerializeField] private TMP_InputField _inputField;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _backButton;
        [SerializeField] private int _maxNicknameLength = 32;
        [Tooltip("Shown after a valid nickname is confirmed (sibling under UIRoot if not assigned).")]
        [SerializeField] private TutorialScreen _tutorialScreen;

        private bool _clickListenersAdded;

        public void SetRuntimeRefs(TMP_InputField input, Button confirm, Button back)
        {
            _inputField = input;
            _confirmButton = confirm;
            _backButton = back;
            TryAddListeners();
        }

        protected override void Awake()
        {
            base.Awake();
            if (_tutorialScreen == null)
            {
                _tutorialScreen = LeaderboardUiRuntimeBuilder.FindChildScreen(transform.parent, "TutorialScreen") as TutorialScreen;
            }

            TryAddListeners();
        }

        private void TryAddListeners()
        {
            if (_clickListenersAdded) return;
            if (_confirmButton == null || _backButton == null) return;
            _clickListenersAdded = true;
            _confirmButton.onClick.AddListener(OnConfirm);
            _backButton.onClick.AddListener(OnBack);
            if (_inputField != null) _inputField.characterLimit = _maxNicknameLength;
        }

        protected override void OnShow()
        {
            if (_inputField == null) return;
            var hint = SaveManager.HasInstance ? SaveManager.Instance.LastNickname : string.Empty;
            _inputField.text = hint;
            _inputField.ActivateInputField();
            _inputField.Select();
        }

        private void OnConfirm()
        {
            if (_inputField == null) return;
            var raw = _inputField.text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(raw)) return;

            if (PlayerSession.HasInstance) PlayerSession.Instance.SetNickname(raw);
            if (SaveManager.HasInstance) SaveManager.Instance.LastNickname = raw;

            if (_tutorialScreen != null && UIManager.HasInstance)
            {
                UIManager.Instance.Push(_tutorialScreen);
                return;
            }

            if (SceneLoader.HasInstance) SceneLoader.Instance.LoadGame();
        }

        private void OnBack()
        {
            if (UIManager.HasInstance) UIManager.Instance.Pop();
        }
    }
}
