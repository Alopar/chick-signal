using LudumDare.Template.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace LudumDare.Template.UI
{
    public class TutorialScreen : UIScreen
    {
        [SerializeField] private Button _playButton;

        private bool _listenersAdded;

        public void SetRuntimeRefs(Button play)
        {
            _playButton = play;
            TryAddListeners();
        }

        protected override void Awake()
        {
            base.Awake();
            TryAddListeners();
        }

        private void TryAddListeners()
        {
            if (_listenersAdded || _playButton == null) return;
            _listenersAdded = true;
            _playButton.onClick.AddListener(OnPlay);
        }

        private void OnPlay()
        {
            if (SceneLoader.HasInstance) SceneLoader.Instance.LoadGame();
        }
    }
}
