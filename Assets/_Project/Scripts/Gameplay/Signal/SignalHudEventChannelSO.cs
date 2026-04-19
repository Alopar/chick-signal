using System;
using UnityEngine;

namespace LudumDare.Template.Gameplay.Signal
{
    public readonly struct SignalHudSnapshot
    {
        public readonly float NestCharge01;
        public readonly float NestHp01;
        public readonly float NestSatiety01;
        public readonly int NestLevel;
        public readonly int WaveDisplayIndex;
        public readonly int EnemyCount;
        public readonly float DashReady01;
        public readonly float TrapSlowBar01;
        public readonly float TrapAttractBar01;

        public SignalHudSnapshot(
            float nestCharge01,
            float nestHp01,
            float nestSatiety01,
            int nestLevel,
            int waveDisplayIndex,
            int enemyCount,
            float dashReady01,
            float trapSlowBar01,
            float trapAttractBar01)
        {
            NestCharge01 = nestCharge01;
            NestHp01 = nestHp01;
            NestSatiety01 = nestSatiety01;
            NestLevel = nestLevel;
            WaveDisplayIndex = waveDisplayIndex;
            EnemyCount = enemyCount;
            DashReady01 = dashReady01;
            TrapSlowBar01 = trapSlowBar01;
            TrapAttractBar01 = trapAttractBar01;
        }
    }

    [CreateAssetMenu(menuName = "LudumDare/Signal/HUD Channel", fileName = "OnSignalHudUpdated")]
    public sealed class SignalHudEventChannelSO : ScriptableObject
    {
        public event Action<SignalHudSnapshot> OnHudUpdated;

        public void Raise(SignalHudSnapshot snapshot) => OnHudUpdated?.Invoke(snapshot);
    }
}
