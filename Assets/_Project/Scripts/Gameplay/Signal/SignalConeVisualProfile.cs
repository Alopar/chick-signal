using UnityEngine;

namespace LudumDare.Template.Gameplay.Signal
{
    /// <summary>
    /// Ассет с пресетом визуала конуса. Создание: Project → Create → Gameplay → Signal → Cone Visual Profile.
    /// Перетащите на поле Load From Profile у <see cref="SignalConeVisualSettings"/> — значения подставятся при старте сцены.
    /// </summary>
    [CreateAssetMenu(fileName = "SignalConeVisualProfile", menuName = "Gameplay/Signal/Cone Visual Profile", order = 0)]
    public sealed class SignalConeVisualProfile : ScriptableObject
    {
        [Header("Цвета: сигнал")]
        public Color SignalConeBase = new(1f, 0.86f, 0.31f, 0.28f);
        public Color SignalConeWave = new(1f, 0.98f, 0.75f, 1f);

        [Header("Цвета: контрсигнал")]
        public Color RepelConeBase = new(1f, 0.45f, 0.42f, 0.24f);
        public Color RepelConeWave = new(1f, 0.75f, 0.72f, 1f);

        [Header("Сортировка 2D")]
        public int ConeSortingOrder = -8;

        [Header("Шейдер: волны")]
        public float ConeWaveSpeed = 4f;
        public float ConeWaveBands = 9f;
        [Range(0f, 1f)] public float ConeWaveMix = 0.55f;
        [Range(1f, 10f)] public float ConeWavePeakPower = 3.5f;
        [Range(0f, 1f)] public float ConeWaveValleyAlpha = 0.22f;
        [Range(0.2f, 1f)] public float ConeRippleContrast = 0.85f;

        [Header("Шейдер: конус и края")]
        [Range(0.001f, 0.2f)] public float ConeEdgeSoftRadians = 0.04f;
        [Range(0.01f, 0.45f)] public float ConeRadialEdgeSoft = 0.12f;
        [Range(0f, 0.55f)] public float ConeOuterEdgeFeather = 0.28f;
        [Range(0f, 0.2f)] public float ConeRadialEdgeBleed = 0.06f;

        [Header("Шейдер: прочее")]
        [Range(0.15f, 1.5f)] public float ConeAlphaScale = 0.55f;
        [Range(0f, 8f)] public float ConeCenterGlow = 2.2f;
    }
}
