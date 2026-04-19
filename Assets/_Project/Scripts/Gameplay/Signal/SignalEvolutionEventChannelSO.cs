using System;
using UnityEngine;

namespace LudumDare.Template.Gameplay.Signal
{
    [CreateAssetMenu(menuName = "LudumDare/Signal/Evolution Modal Channel", fileName = "OnSignalEvolutionModal")]
    public sealed class SignalEvolutionEventChannelSO : ScriptableObject
    {
        public event Action<bool> OnVisibilityChanged;

        public void Raise(bool visible) => OnVisibilityChanged?.Invoke(visible);
    }
}
