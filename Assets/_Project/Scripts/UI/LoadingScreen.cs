using LudumDare.Template.Events;
using UnityEngine;

namespace LudumDare.Template.UI
{
    public class LoadingScreen : UIScreen
    {
        [SerializeField] private VoidEventChannelSO _onLoadStart;
        [SerializeField] private VoidEventChannelSO _onLoadComplete;

        protected override void Awake()
        {
            base.Awake();
            if (_onLoadStart != null)    _onLoadStart.OnEventRaised    += Show;
            if (_onLoadComplete != null) _onLoadComplete.OnEventRaised += Hide;
        }

        protected override void OnDestroy()
        {
            if (_onLoadStart != null)    _onLoadStart.OnEventRaised    -= Show;
            if (_onLoadComplete != null) _onLoadComplete.OnEventRaised -= Hide;
            base.OnDestroy();
        }
    }
}
