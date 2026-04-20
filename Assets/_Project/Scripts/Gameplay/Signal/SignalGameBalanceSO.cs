using System;
using System.Collections.Generic;
using UnityEngine;

namespace LudumDare.Template.Gameplay.Signal
{
    /// <summary>
    /// Геймплейный баланс SIGNAL (логические единицы как в migration-data/signal_game.html).
    /// Секция draw, bob/flicker/trail и чисто визуальные поля не включены.
    /// </summary>
    [CreateAssetMenu(menuName = "LudumDare/Signal/Game Balance", fileName = "SignalGameBalance")]
    public sealed class SignalGameBalanceSO : ScriptableObject
    {
        [Header("Arena (logical pixels, как canvas 900×600)")]
        [Min(1f)] public float ReferenceCanvasWidth = 900f;
        [Min(1f)] public float ReferenceCanvasHeight = 600f;
        [Tooltip("Запасной масштаб, если нет ортокамеры: 1 world unit = N logical pixels. Иначе масштаб берётся из Orthographic Size камеры в сцене, чтобы по вертикали влезала ReferenceCanvasHeight.")]
        [Min(0.0001f)] public float LogicalPixelsPerWorldUnit = 100f;

        [Header("Simulation")]
        [Min(0.001f)] public float LoopMaxDeltaTime = 0.05f;

        public PlayerBalance Player = new();
        public SignalConeBalance Signal = new();
        public DebuffBalance Debuff = new();
        public NestBalance Nest = new();
        public TrapsBalance Traps = new();
        public SpawnBalance Spawn = new();
        public EnemyBalance Enemy = new();
        public AllyBalance Ally = new();
        public WinBalance Win = new();

        [Header("Debug / optional rules (как чекбоксы в HTML)")]
        public bool CheatNoRedEnemyNestDamage;
        public bool OptionNestHealFromGreen;

        [Header("Dead / unused in original HTML (оставлено для ясности)")]
        [Tooltip("В HTML не выставляется — глушение сигнала при касании врага не работало.")]
        public bool DocumentSignalJamNotImplemented = true;
        [Tooltip("В HTML moveSlowTimer нигде не задаётся.")]
        public bool DocumentEnemyContactSlowNotImplemented = true;
        [Tooltip("В HTML урон по HP игрока отсутствует.")]
        public bool DocumentPlayerDamageNotImplemented = true;
    }

    [Serializable]
    public struct PlayerBalance
    {
        public float StartDx;
        public float Speed;
        public float Hp;
        public float MaxHp;
        public float SignalPower;
        public float SignalRadius;
        public float Radius;
        public float SignalJamOnHit;
        public float DashDistance;
        public float DashDuration;
        public float DashCooldown;
        public float RepelSpeedMult;
    }

    [Serializable]
    public struct SignalConeBalance
    {
        [Tooltip("Полная ширина конуса в радианах (π/4 в оригинале).")]
        public float ConeAngleRadians;
        public float StrengthScale;
        public float NpcWakeDuration;
    }

    [Serializable]
    public struct DebuffBalance
    {
        public float SlowMult;
        public float SlowDuration;
    }

    [Serializable]
    public struct NestBalance
    {
        public float StartDx;
        public float Hp;
        public float MaxHp;
        public int InitialLevel;
        public float InitialChargeMax;
        public float HealRadius;
        public float HealBase;
        public float HealProximityBonus;
        public float SignalPower;
        public float Radius;
        public float PulseInterval;
        public float PulseExpandSpeed;
        public float PulseMaxRadius;
        public int MaxLevel;
        public float ChargeBase;
        public float ChargePerLevel;
        public float LevelSignalPowerBonus;
        public float AllyDeliveryPlayerMaxHpBonus;
        public int AllyDeliveryPlayerMaxHpFromLevel;
        public float AoeCooldown;
        public float AoeRadius;
        public int AoeUnlockLevel;
        public float AbsorptionDuration;
        public float SatiationMax;
        public float RadiusPerLevel;
    }

    [Serializable]
    public struct TrapsBalance
    {
        public int Count;
        public float RadiusMin;
        public float RadiusMax;
        public float RingInner;
        public float RingOuter;
        public float MinGapBetween;
        public float SectorEdgeMargin;
        public float SlowMult;
        public float PlayerPlacedRadius;
        public float PlayerPlacedDuration;
        public int PlayerTrapMaxCharges;
        public float PlayerTrapRechargeSlow;
        public float PlayerTrapRechargeAttract;
        public float AttractPullSpeedMult;
    }

    [Serializable]
    public struct SpawnBalance
    {
        public float EdgeMargin;
        public int StartingAllies;
        public List<SignalWaveEntry> Waves;
    }

    [Serializable]
    public struct SignalWaveEntry
    {
        public float Duration;
        public int Red;
        public int Green;
    }

    [Serializable]
    public struct EnemyBalance
    {
        public float SpeedMin;
        public float SpeedRand;
        public float NestDamageRed;
        public float NestDamageGreen;
        public float SatiationRed;
        public float SatiationGreen;
        [Min(0)] public int ScoreRed;
        [Min(0)] public int ScoreGreen;
        public float Radius;
    }

    [Serializable]
    public struct AllyBalance
    {
        public float SpeedMin;
        public float SpeedRand;
        public float Radius;
    }

    [Serializable]
    public struct WinBalance
    {
    }

    public static class SignalBalanceDefaults
    {
        public static void ApplyHtmlDefaults(SignalGameBalanceSO s)
        {
            s.ReferenceCanvasWidth = 900f;
            s.ReferenceCanvasHeight = 600f;
            s.LogicalPixelsPerWorldUnit = 100f;
            s.LoopMaxDeltaTime = 0.05f;

            s.Player = new PlayerBalance
            {
                StartDx = 200f,
                Speed = 140f,
                Hp = 100f,
                MaxHp = 100f,
                SignalPower = 400f,
                SignalRadius = 160f,
                Radius = 14f,
                SignalJamOnHit = 3.5f,
                DashDistance = 130f,
                DashDuration = 0.14f,
                DashCooldown = 1.15f,
                RepelSpeedMult = 1.15f,
            };

            s.Signal = new SignalConeBalance
            {
                ConeAngleRadians = Mathf.PI / 4f,
                StrengthScale = 10000f,
                NpcWakeDuration = 0.7f,
            };

            s.Debuff = new DebuffBalance { SlowMult = 0.05f, SlowDuration = 1.5f };

            s.Nest = new NestBalance
            {
                StartDx = -120f,
                Hp = 250f,
                MaxHp = 250f,
                InitialLevel = 1,
                InitialChargeMax = 5f,
                HealRadius = 5f,
                HealBase = 10f,
                HealProximityBonus = 8f,
                SignalPower = 150f,
                Radius = 16f,
                PulseInterval = 1.5f,
                PulseExpandSpeed = 300f,
                PulseMaxRadius = 900f,
                MaxLevel = 14,
                ChargeBase = 5f,
                ChargePerLevel = 10f,
                LevelSignalPowerBonus = 60f,
                AllyDeliveryPlayerMaxHpBonus = 20f,
                AllyDeliveryPlayerMaxHpFromLevel = 2,
                AoeCooldown = 2.5f,
                AoeRadius = 120f,
                AoeUnlockLevel = 99,
                AbsorptionDuration = 10f,
                SatiationMax = 10f,
                RadiusPerLevel = 8f,
            };

            s.Traps = new TrapsBalance
            {
                Count = 1,
                RadiusMin = 65f,
                RadiusMax = 80f,
                RingInner = 250f,
                RingOuter = 400f,
                MinGapBetween = 12f,
                SectorEdgeMargin = 0.12f,
                SlowMult = 0.15f,
                PlayerPlacedRadius = 55f,
                PlayerPlacedDuration = 6f,
                PlayerTrapMaxCharges = 1,
                PlayerTrapRechargeSlow = 10.2f,
                PlayerTrapRechargeAttract = 10.2f,
                AttractPullSpeedMult = 0.5f,
            };

            s.Spawn = new SpawnBalance
            {
                EdgeMargin = 20f,
                StartingAllies = 0,
                Waves = new List<SignalWaveEntry>
                {
                    new() { Duration = 10f, Red = 2, Green = 4 },
                    new() { Duration = 25f, Red = 5, Green = 5 },
                    new() { Duration = 25f, Red = 3, Green = 7 },
                    new() { Duration = 30f, Red = 5, Green = 9 },
                    new() { Duration = 35f, Red = 4, Green = 9 },
                    new() { Duration = 45f, Red = 5, Green = 12 },
                    new() { Duration = 50f, Red = 7, Green = 13 },
                    new() { Duration = 60f, Red = 6, Green = 15 },
                    new() { Duration = 75f, Red = 11, Green = 12 },
                    new() { Duration = 80f, Red = 7, Green = 100 },
                },
            };

            s.Enemy = new EnemyBalance
            {
                SpeedMin = 55f,
                SpeedRand = 20f,
                NestDamageRed = 10f,
                NestDamageGreen = 0f,
                SatiationRed = 4f,
                SatiationGreen = 2f,
                ScoreRed = 1,
                ScoreGreen = 1,
                Radius = 9f,
            };

            s.Ally = new AllyBalance { SpeedMin = 60f, SpeedRand = 20f, Radius = 9f };

            s.Win = new WinBalance();
        }
    }
}
