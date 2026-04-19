using UnityEngine;

namespace LudumDare.Template.Gameplay.Signal
{
    /// <summary>Имена свойств шейдера Project/SignalConeWedge для MPB и Material.</summary>
    public static class SignalConeVisualShaders
    {
        public static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        public static readonly int WaveColorId = Shader.PropertyToID("_WaveColor");
        public static readonly int HalfAngleRadId = Shader.PropertyToID("_HalfAngleRad");
        public static readonly int EdgeSoftRadiansId = Shader.PropertyToID("_EdgeSoftRadians");
        public static readonly int RadialEdgeSoftId = Shader.PropertyToID("_RadialEdgeSoft");
        public static readonly int OuterEdgeFeatherId = Shader.PropertyToID("_OuterEdgeFeather");
        public static readonly int RadialEdgeBleedId = Shader.PropertyToID("_RadialEdgeBleed");
        public static readonly int CenterGlowId = Shader.PropertyToID("_CenterGlow");
        public static readonly int WaveBandsId = Shader.PropertyToID("_WaveBands");
        public static readonly int WaveSpeedId = Shader.PropertyToID("_WaveSpeed");
        public static readonly int WaveMixId = Shader.PropertyToID("_WaveMix");
        public static readonly int WavePeakPowerId = Shader.PropertyToID("_WavePeakPower");
        public static readonly int WaveValleyAlphaId = Shader.PropertyToID("_WaveValleyAlpha");
        public static readonly int RippleContrastId = Shader.PropertyToID("_RippleContrast");
        public static readonly int AlphaScaleId = Shader.PropertyToID("_AlphaScale");
    }
}
