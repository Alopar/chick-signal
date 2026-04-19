using LudumDare.Template.Events;
using LudumDare.Template.Managers;
using TMPro;
using UnityEngine;

namespace LudumDare.Template.UI
{
    public class HUDScreen : UIScreen
    {
        [SerializeField] private IntEventChannelSO _scoreChannel;
        [SerializeField] private TMP_Text _scoreLabel;

        protected override void Awake()
        {
            base.Awake();
            if (_scoreChannel != null) _scoreChannel.OnEventRaised += HandleScore;
        }

        protected override void OnDestroy()
        {
            if (_scoreChannel != null) _scoreChannel.OnEventRaised -= HandleScore;
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
    }
}
