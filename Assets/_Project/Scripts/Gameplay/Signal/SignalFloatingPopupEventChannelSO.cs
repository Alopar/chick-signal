using System;
using UnityEngine;

namespace LudumDare.Template.Gameplay.Signal
{
    public enum SignalFloatingPopupType
    {
        Damage,
        HpGain,
        ChargeGain,
        FoodGain,
    }

    [Serializable]
    public readonly struct SignalFloatingPopupEvent
    {
        public readonly SignalFloatingPopupType Type;
        public readonly float Amount;
        public readonly Vector3 WorldPosition;

        public SignalFloatingPopupEvent(SignalFloatingPopupType type, float amount, Vector3 worldPosition)
        {
            Type = type;
            Amount = amount;
            WorldPosition = worldPosition;
        }
    }

    [CreateAssetMenu(menuName = "LudumDare/Signal/Floating Popup Channel", fileName = "OnSignalFloatingPopup")]
    public sealed class SignalFloatingPopupEventChannelSO : ScriptableObject
    {
        public event Action<SignalFloatingPopupEvent> OnPopupRequested;

        public void Raise(SignalFloatingPopupEvent popupEvent) => OnPopupRequested?.Invoke(popupEvent);
    }
}
