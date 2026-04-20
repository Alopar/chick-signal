using System;
using UnityEngine;

namespace LudumDare.Template.Gameplay.Signal
{
    public enum SignalInfoToastType
    {
        WaveStarted,
        ComboReached,
        WaveScoreBonus,
    }

    [Serializable]
    public readonly struct SignalInfoToastEvent
    {
        public readonly SignalInfoToastType Type;
        public readonly int WaveNumber;
        public readonly int ComboMultiplier;
        public readonly int ScoreAmount;

        public SignalInfoToastEvent(SignalInfoToastType type, int waveNumber = 0, int comboMultiplier = 0, int scoreAmount = 0)
        {
            Type = type;
            WaveNumber = waveNumber;
            ComboMultiplier = comboMultiplier;
            ScoreAmount = scoreAmount;
        }
    }

    [CreateAssetMenu(menuName = "LudumDare/Signal/Info Toast Channel", fileName = "OnSignalInfoToast")]
    public sealed class SignalInfoToastEventChannelSO : ScriptableObject
    {
        public event Action<SignalInfoToastEvent> OnToastRequested;

        public void Raise(SignalInfoToastEvent toastEvent) => OnToastRequested?.Invoke(toastEvent);
    }
}
