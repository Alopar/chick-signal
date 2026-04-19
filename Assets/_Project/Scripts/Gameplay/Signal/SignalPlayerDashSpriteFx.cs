using UnityEngine;

namespace LudumDare.Template.Gameplay.Signal
{
    /// <summary>
    /// Параметры шейдера Project/SpriteDashEcho: интенсивность дэша задаётся во время рывка, остальное — из инспектора (MPB).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SignalPlayerDashSpriteFx : MonoBehaviour
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int DashIntensityId = Shader.PropertyToID("_DashIntensity");
        private static readonly int DashDirId = Shader.PropertyToID("_DashDir");
        private static readonly int EchoSpreadId = Shader.PropertyToID("_EchoSpread");
        private static readonly int EchoStepScaleId = Shader.PropertyToID("_EchoStepScale");
        private static readonly int EchoStrengthId = Shader.PropertyToID("_EchoStrength");
        private static readonly int EchoTintId = Shader.PropertyToID("_EchoTint");
        private static readonly int ChromaticAmtId = Shader.PropertyToID("_ChromaticAmt");
        private static readonly int ChromaticSpreadId = Shader.PropertyToID("_ChromaticSpread");
        private static readonly int GlowId = Shader.PropertyToID("_Glow");

        [SerializeField] private SignalGameController _controller;
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [Header("Timing")]
        [Tooltip("Усиливает пик в начале дэша (>1 — сильнее в начале).")]
        [SerializeField, Min(0.01f)] private float _intensityCurvePower = 0.9f;
        [Tooltip("Множитель к фазе дэша (0…1) перед подачей в шейдер.")]
        [SerializeField, Range(0f, 2f)] private float _dashIntensityScale = 1f;

        [Header("Echo / trail")]
        [Tooltip("Базовый размаз следа по UV; больше — длиннее «хвост».")]
        [SerializeField, Range(0f, 0.85f)] private float _echoSpread = 0.12f;
        [Tooltip("Расстояние между слоями эха; больше — длиннее видимый след.")]
        [SerializeField, Range(0.08f, 0.65f)] private float _echoStepScale = 0.28f;
        [SerializeField, Range(0f, 2f)] private float _echoStrength = 0.85f;
        [SerializeField] private Color _echoTint = new(0.55f, 0.92f, 1f, 1f);

        [Header("Chromatic")]
        [SerializeField, Range(0f, 1f)] private float _chromaticAmount = 0.35f;
        [Tooltip("Смещение каналов по UV (размаз хроматики).")]
        [SerializeField, Range(0f, 0.06f)] private float _chromaticSpread = 0.014f;

        [Header("Glow")]
        [SerializeField, Range(0f, 1.2f)] private float _glow = 0.18f;

        [Header("Tint")]
        [Tooltip("Умножение текстуры спрайта (как _Color в материале).")]
        [SerializeField] private Color _spriteTint = Color.white;

        private MaterialPropertyBlock _mpb;

        private void Awake()
        {
            if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
            _mpb = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            if (_controller == null) _controller = FindAnyObjectByType<SignalGameController>();
        }

        private void LateUpdate()
        {
            if (_spriteRenderer == null) return;

            _spriteRenderer.GetPropertyBlock(_mpb);

            _mpb.SetColor(ColorId, _spriteTint);
            _mpb.SetFloat(EchoSpreadId, _echoSpread);
            _mpb.SetFloat(EchoStepScaleId, _echoStepScale);
            _mpb.SetFloat(EchoStrengthId, _echoStrength);
            _mpb.SetColor(EchoTintId, _echoTint);
            _mpb.SetFloat(ChromaticAmtId, _chromaticAmount);
            _mpb.SetFloat(ChromaticSpreadId, _chromaticSpread);
            _mpb.SetFloat(GlowId, _glow);

            if (_controller == null || !_controller.PlayerIsDashing)
            {
                _mpb.SetFloat(DashIntensityId, 0f);
                _mpb.SetVector(DashDirId, Vector4.zero);
                _spriteRenderer.SetPropertyBlock(_mpb);
                return;
            }

            float t = _controller.PlayerDashIntensity01;
            if (_intensityCurvePower > 0.0001f)
                t = Mathf.Pow(t, _intensityCurvePower);
            t = Mathf.Clamp01(t * _dashIntensityScale);

            Vector2 world = _controller.PlayerDashDirectionWorld;
            float tx = _spriteRenderer.flipX ? -world.x : world.x;
            float ty = world.y;
            Vector2 uvDir = new Vector2(tx, ty);
            if (uvDir.sqrMagnitude > 1e-6f)
                uvDir.Normalize();
            else
                uvDir = Vector2.right;

            _mpb.SetFloat(DashIntensityId, t);
            _mpb.SetVector(DashDirId, new Vector4(uvDir.x, uvDir.y, 0f, 0f));
            _spriteRenderer.SetPropertyBlock(_mpb);
        }
    }
}
