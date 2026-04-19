using UnityEngine;

namespace LudumDare.Template.Gameplay.Signal
{
    /// <summary>
    /// Все параметры визуала конуса сигнала (шейдер + сортировка).
    /// Повесьте на тот же GameObject, что и <see cref="SignalGameplayView"/>, и настраивайте в инспекторе.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Gameplay/Signal/Signal Cone Visual Settings")]
    [DefaultExecutionOrder(-40)]
    public sealed class SignalConeVisualSettings : MonoBehaviour
    {
        [Header("Ссылки")]
        [SerializeField] private Sprite _coneSprite;
        [Tooltip("Если задан — при Awake копирует значения в поля ниже (можно править дальше на компоненте).")]
        [SerializeField] private SignalConeVisualProfile _loadFromProfile;

        [Header("Цвета: сигнал")]
        [SerializeField] private Color _signalConeBase = new(1f, 0.86f, 0.31f, 0.28f);
        [SerializeField] private Color _signalConeWave = new(1f, 0.98f, 0.75f, 1f);

        [Header("Цвета: контрсигнал")]
        [SerializeField] private Color _repelConeBase = new(1f, 0.45f, 0.42f, 0.24f);
        [SerializeField] private Color _repelConeWave = new(1f, 0.75f, 0.72f, 1f);

        [Header("Сортировка 2D")]
        [SerializeField] private int _coneSortingOrder = -8;

        [Header("Шейдер: волны")]
        [SerializeField] private float _coneWaveSpeed = 4f;
        [SerializeField] private float _coneWaveBands = 9f;
        [SerializeField] [Range(0f, 1f)] private float _coneWaveMix = 0.55f;
        [SerializeField] [Range(1f, 10f)] private float _coneWavePeakPower = 3.5f;
        [SerializeField] [Range(0f, 1f)] private float _coneWaveValleyAlpha = 0.22f;
        [SerializeField] [Range(0.2f, 1f)] private float _coneRippleContrast = 0.85f;

        [Header("Шейдер: конус и края")]
        [SerializeField] [Range(0.001f, 0.2f)] private float _coneEdgeSoftRadians = 0.04f;
        [SerializeField] [Range(0.01f, 0.45f)] private float _coneRadialEdgeSoft = 0.12f;
        [SerializeField] [Range(0f, 0.55f)] private float _coneOuterEdgeFeather = 0.28f;
        [SerializeField] [Range(0f, 0.2f)] private float _coneRadialEdgeBleed = 0.06f;

        [Header("Шейдер: прочее")]
        [SerializeField] [Range(0.15f, 1.5f)] private float _coneAlphaScale = 0.55f;
        [SerializeField] [Range(0f, 8f)] private float _coneCenterGlow = 2.2f;

        public Sprite ConeSprite => _coneSprite;
        public Color SignalConeBase => _signalConeBase;
        public Color SignalConeWave => _signalConeWave;
        public Color RepelConeBase => _repelConeBase;
        public Color RepelConeWave => _repelConeWave;
        public int ConeSortingOrder => _coneSortingOrder;

        public void ApplyStaticToMaterial(Material m)
        {
            if (m == null) return;
            m.SetFloat(SignalConeVisualShaders.WaveSpeedId, _coneWaveSpeed);
            m.SetFloat(SignalConeVisualShaders.WaveBandsId, _coneWaveBands);
            m.SetFloat(SignalConeVisualShaders.WaveMixId, _coneWaveMix);
            m.SetFloat(SignalConeVisualShaders.EdgeSoftRadiansId, _coneEdgeSoftRadians);
            m.SetFloat(SignalConeVisualShaders.RadialEdgeSoftId, _coneRadialEdgeSoft);
            m.SetFloat(SignalConeVisualShaders.OuterEdgeFeatherId, _coneOuterEdgeFeather);
            m.SetFloat(SignalConeVisualShaders.RadialEdgeBleedId, _coneRadialEdgeBleed);
            m.SetFloat(SignalConeVisualShaders.CenterGlowId, _coneCenterGlow);
            m.SetFloat(SignalConeVisualShaders.WavePeakPowerId, _coneWavePeakPower);
            m.SetFloat(SignalConeVisualShaders.WaveValleyAlphaId, _coneWaveValleyAlpha);
            m.SetFloat(SignalConeVisualShaders.RippleContrastId, _coneRippleContrast);
            m.SetFloat(SignalConeVisualShaders.AlphaScaleId, _coneAlphaScale);
        }

        private void Awake()
        {
            if (_loadFromProfile != null)
                CopyFrom(_loadFromProfile);
        }

        public void CopyFrom(SignalConeVisualProfile p)
        {
            if (p == null) return;
            _signalConeBase = p.SignalConeBase;
            _signalConeWave = p.SignalConeWave;
            _repelConeBase = p.RepelConeBase;
            _repelConeWave = p.RepelConeWave;
            _coneSortingOrder = p.ConeSortingOrder;
            _coneWaveSpeed = p.ConeWaveSpeed;
            _coneWaveBands = p.ConeWaveBands;
            _coneWaveMix = p.ConeWaveMix;
            _coneWavePeakPower = p.ConeWavePeakPower;
            _coneWaveValleyAlpha = p.ConeWaveValleyAlpha;
            _coneRippleContrast = p.ConeRippleContrast;
            _coneEdgeSoftRadians = p.ConeEdgeSoftRadians;
            _coneRadialEdgeSoft = p.ConeRadialEdgeSoft;
            _coneOuterEdgeFeather = p.ConeOuterEdgeFeather;
            _coneRadialEdgeBleed = p.ConeRadialEdgeBleed;
            _coneAlphaScale = p.ConeAlphaScale;
            _coneCenterGlow = p.ConeCenterGlow;
        }
    }
}
