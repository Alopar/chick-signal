using LudumDare.Template.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace LudumDare.Template.UI
{
    public class SettingsScreen : UIScreen
    {
        [SerializeField] private Slider _masterSlider;
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Slider _sfxSlider;
        [SerializeField] private Toggle _fullscreenToggle;
        [SerializeField] private Button _backButton;

        protected override void Awake()
        {
            base.Awake();
            if (_masterSlider != null)     _masterSlider.onValueChanged.AddListener(OnMasterChanged);
            if (_musicSlider != null)      _musicSlider.onValueChanged.AddListener(OnMusicChanged);
            if (_sfxSlider != null)        _sfxSlider.onValueChanged.AddListener(OnSfxChanged);
            if (_fullscreenToggle != null) _fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            if (_backButton != null)       _backButton.onClick.AddListener(OnBack);
        }

        protected override void OnShow()
        {
            if (!SaveManager.HasInstance) return;
            var save = SaveManager.Instance;
            if (_masterSlider != null)     _masterSlider.SetValueWithoutNotify(save.MasterVolume);
            if (_musicSlider != null)      _musicSlider.SetValueWithoutNotify(save.MusicVolume);
            if (_sfxSlider != null)        _sfxSlider.SetValueWithoutNotify(save.SfxVolume);
            if (_fullscreenToggle != null) _fullscreenToggle.SetIsOnWithoutNotify(save.Fullscreen);
        }

        private void OnMasterChanged(float v)
        {
            if (AudioManager.HasInstance) AudioManager.Instance.SetMasterVolume(v);
        }

        private void OnMusicChanged(float v)
        {
            if (AudioManager.HasInstance) AudioManager.Instance.SetMusicVolume(v);
        }

        private void OnSfxChanged(float v)
        {
            if (AudioManager.HasInstance) AudioManager.Instance.SetSfxVolume(v);
        }

        private void OnFullscreenChanged(bool value)
        {
            Screen.fullScreen = value;
            if (SaveManager.HasInstance) SaveManager.Instance.Fullscreen = value;
        }

        private void OnBack()
        {
            if (UIManager.HasInstance) UIManager.Instance.Pop();
        }
    }
}
