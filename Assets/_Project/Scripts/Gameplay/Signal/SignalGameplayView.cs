using System.Collections.Generic;
using UnityEngine;

namespace LudumDare.Template.Gameplay.Signal
{
    /// <summary>
    /// Синхронизирует спрайты/линии с состоянием <see cref="SignalGameController"/> (как отрисовка в HTML).
    /// </summary>
    [DefaultExecutionOrder(-30)]
    public sealed class SignalGameplayView : MonoBehaviour
    {
        [SerializeField] private SignalGameController _controller;
        [SerializeField] private Transform _nestAnchor;
        [SerializeField] private GameObject _nestVisualPrefab;
        [SerializeField] private SpriteRenderer _signalCone;
        [SerializeField] private SpriteRenderer _repelCone;
        [SerializeField] private Sprite _coneSprite;
        [SerializeField] private Color _signalConeBase = new(1f, 0.86f, 0.31f, 0.28f);
        [SerializeField] private Color _signalConeWave = new(1f, 0.98f, 0.75f, 1f);
        [SerializeField] private Color _repelConeBase = new(1f, 0.45f, 0.42f, 0.24f);
        [SerializeField] private Color _repelConeWave = new(1f, 0.75f, 0.72f, 1f);
        [SerializeField] private float _coneWaveSpeed = 4f;
        [SerializeField] private float _coneWaveBands = 9f;
        [SerializeField] [Range(0f, 1f)] private float _coneWaveMix = 0.55f;
        [SerializeField] [Range(0.001f, 0.2f)] private float _coneEdgeSoftRadians = 0.04f;
        [SerializeField] [Range(0.01f, 0.45f)] private float _coneRadialEdgeSoft = 0.12f;
        [SerializeField] [Range(0f, 0.55f)] private float _coneOuterEdgeFeather = 0.28f;
        [SerializeField] [Range(0f, 0.2f)] private float _coneRadialEdgeBleed = 0.06f;
        [SerializeField] [Range(1f, 10f)] private float _coneWavePeakPower = 3.5f;
        [SerializeField] [Range(0f, 1f)] private float _coneWaveValleyAlpha = 0.22f;
        [SerializeField] [Range(0.15f, 1.5f)] private float _coneAlphaScale = 0.55f;
        [SerializeField] [Range(0f, 8f)] private float _coneCenterGlow = 2.2f;
        [SerializeField] [Range(0.2f, 1f)] private float _coneRippleContrast = 0.85f;
        [SerializeField] private int _coneSortingOrder = -8;
        [SerializeField] private Color _allyColor = new(0.27f, 1f, 0.53f, 0.9f);
        [SerializeField] private Color _enemyColor = new(1f, 0.26f, 0.4f, 0.9f);
        [SerializeField] private Sprite _mudTrapSprite;
        [SerializeField] private Sprite _flyTrapSprite;
        [SerializeField] private Color _mudTrapTint = new(1f, 1f, 1f, 0.88f);
        [SerializeField] private Color _flyTrapTint = new(1f, 1f, 1f, 0.88f);
        [SerializeField] private Color _pulseColor = new(0.27f, 0.8f, 1f, 0.5f);

        private Transform _unitsRoot;
        private Transform _fxRoot;
        private readonly List<SpriteRenderer> _allySprites = new();
        private readonly List<SpriteRenderer> _enemySprites = new();
        private readonly List<LineRenderer> _pulseRings = new();
        private readonly List<SpriteRenderer> _staticTrapRings = new();
        private readonly List<SpriteRenderer> _playerTrapRings = new();
        private SignalNestVisual _nestVisual;
        private Material _coneMaterial;
        private MaterialPropertyBlock _mpbSignal;
        private MaterialPropertyBlock _mpbRepel;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int WaveColorId = Shader.PropertyToID("_WaveColor");
        private static readonly int HalfAngleRadId = Shader.PropertyToID("_HalfAngleRad");
        private static readonly int EdgeSoftRadiansId = Shader.PropertyToID("_EdgeSoftRadians");
        private static readonly int RadialEdgeSoftId = Shader.PropertyToID("_RadialEdgeSoft");
        private static readonly int OuterEdgeFeatherId = Shader.PropertyToID("_OuterEdgeFeather");
        private static readonly int RadialEdgeBleedId = Shader.PropertyToID("_RadialEdgeBleed");
        private static readonly int CenterGlowId = Shader.PropertyToID("_CenterGlow");
        private static readonly int WaveBandsId = Shader.PropertyToID("_WaveBands");
        private static readonly int WaveSpeedId = Shader.PropertyToID("_WaveSpeed");
        private static readonly int WaveMixId = Shader.PropertyToID("_WaveMix");
        private static readonly int WavePeakPowerId = Shader.PropertyToID("_WavePeakPower");
        private static readonly int WaveValleyAlphaId = Shader.PropertyToID("_WaveValleyAlpha");
        private static readonly int RippleContrastId = Shader.PropertyToID("_RippleContrast");
        private static readonly int AlphaScaleId = Shader.PropertyToID("_AlphaScale");

        private void Awake()
        {
            if (_controller == null) _controller = GetComponent<SignalGameController>();
            if (_nestAnchor == null)
            {
                var n = new GameObject("Nest");
                n.transform.SetParent(transform, false);
                _nestAnchor = n.transform;
            }

            if (_unitsRoot == null)
            {
                var u = new GameObject("Units");
                u.transform.SetParent(transform, false);
                _unitsRoot = u.transform;
            }

            if (_fxRoot == null)
            {
                var f = new GameObject("Fx");
                f.transform.SetParent(transform, false);
                _fxRoot = f.transform;
            }

            EnsureNestVisual();
            EnsureConeRenderers();
        }

        private void EnsureNestVisual()
        {
            if (_nestVisualPrefab == null)
            {
                Debug.LogWarning($"{nameof(SignalGameplayView)}: не назначен префаб гнезда ({nameof(_nestVisualPrefab)}).");
                return;
            }

            for (int i = _nestAnchor.childCount - 1; i >= 0; i--)
                Destroy(_nestAnchor.GetChild(i).gameObject);

            var instance = Instantiate(_nestVisualPrefab, _nestAnchor, false);
            instance.name = _nestVisualPrefab.name;
            _nestVisual = instance.GetComponent<SignalNestVisual>();
            if (_nestVisual == null) _nestVisual = instance.AddComponent<SignalNestVisual>();
            if (_nestVisual.SpriteRenderer == null)
                Debug.LogWarning($"{nameof(SignalGameplayView)}: у префаба гнезда нет {nameof(SpriteRenderer)}.");
            _nestAnchor.localScale = Vector3.one;
        }

        private void EnsureConeRenderers()
        {
            Shader coneShader = Shader.Find("Project/SignalConeWedge");
            if (coneShader == null)
            {
                Debug.LogError($"{nameof(SignalGameplayView)}: не найден шейдер Project/SignalConeWedge.");
                return;
            }

            _coneMaterial = new Material(coneShader);
            ApplyConeMaterialStaticParams();
            _mpbSignal = new MaterialPropertyBlock();
            _mpbRepel = new MaterialPropertyBlock();

            if (_signalCone == null)
            {
                var go = new GameObject("SignalCone");
                go.transform.SetParent(transform, false);
                _signalCone = go.AddComponent<SpriteRenderer>();
                SetupConeSpriteRenderer(_signalCone);
            }

            if (_repelCone == null)
            {
                var go = new GameObject("RepelCone");
                go.transform.SetParent(transform, false);
                _repelCone = go.AddComponent<SpriteRenderer>();
                SetupConeSpriteRenderer(_repelCone);
            }

            _signalCone.sharedMaterial = _coneMaterial;
            _repelCone.sharedMaterial = _coneMaterial;
            _signalCone.sortingOrder = _coneSortingOrder;
            _repelCone.sortingOrder = _coneSortingOrder - 1;
        }

        private void ApplyConeMaterialStaticParams()
        {
            if (_coneMaterial == null) return;
            _coneMaterial.SetFloat(WaveSpeedId, _coneWaveSpeed);
            _coneMaterial.SetFloat(WaveBandsId, _coneWaveBands);
            _coneMaterial.SetFloat(WaveMixId, _coneWaveMix);
            _coneMaterial.SetFloat(EdgeSoftRadiansId, _coneEdgeSoftRadians);
            _coneMaterial.SetFloat(RadialEdgeSoftId, _coneRadialEdgeSoft);
            _coneMaterial.SetFloat(OuterEdgeFeatherId, _coneOuterEdgeFeather);
            _coneMaterial.SetFloat(RadialEdgeBleedId, _coneRadialEdgeBleed);
            _coneMaterial.SetFloat(CenterGlowId, _coneCenterGlow);
            _coneMaterial.SetFloat(WavePeakPowerId, _coneWavePeakPower);
            _coneMaterial.SetFloat(WaveValleyAlphaId, _coneWaveValleyAlpha);
            _coneMaterial.SetFloat(RippleContrastId, _coneRippleContrast);
            _coneMaterial.SetFloat(AlphaScaleId, _coneAlphaScale);
        }

        private void SetupConeSpriteRenderer(SpriteRenderer sr)
        {
            sr.sprite = _coneSprite != null ? _coneSprite : GetDefaultSprite();
            sr.color = Color.white;
            sr.sortingOrder = _coneSortingOrder;
        }

        private void LateUpdate()
        {
            if (_controller == null) return;
            if (_controller.Balance == null) return;

            SyncNest();
            SyncUnits();
            SyncPulses();
            SyncStaticTraps();
            SyncPlayerTraps();
            SyncCones();
        }

        private void SyncNest()
        {
            if (_nestVisual == null) return;

            _controller.GetNest(out float lx, out float ly, out _, out bool absorbing, out _);
            Vector3 p = _controller.LogicalToWorldPublic(lx, ly);
            _nestAnchor.position = new Vector3(p.x, p.y, 0f);
            _nestVisual.SetAbsorbing(absorbing);
        }

        private void SyncUnits()
        {
            int ac = _controller.GetAllyCount();
            while (_allySprites.Count < ac) _allySprites.Add(CreateDot(_unitsRoot, "Ally", _allyColor, 4));
            while (_allySprites.Count > ac)
            {
                int last = _allySprites.Count - 1;
                Destroy(_allySprites[last].gameObject);
                _allySprites.RemoveAt(last);
            }

            for (int i = 0; i < ac; i++)
            {
                _controller.GetAlly(i, out float lx, out float ly, out float rad, out float mt);
                ApplyDot(_allySprites[i], lx, ly, rad, mt > 0f);
            }

            int ec = _controller.GetEnemyCount();
            while (_enemySprites.Count < ec) _enemySprites.Add(CreateDot(_unitsRoot, "Enemy", _enemyColor, 5));
            while (_enemySprites.Count > ec)
            {
                int last = _enemySprites.Count - 1;
                Destroy(_enemySprites[last].gameObject);
                _enemySprites.RemoveAt(last);
            }

            for (int i = 0; i < ec; i++)
            {
                _controller.GetEnemy(i, out float lx, out float ly, out float rad, out float mt, out SignalNpcKind kind);
                var col = kind == SignalNpcKind.Red ? _enemyColor : _allyColor;
                _enemySprites[i].color = col;
                ApplyDot(_enemySprites[i], lx, ly, rad, mt > 0f);
            }
        }

        private SpriteRenderer CreateDot(Transform parent, string name, Color c, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetDefaultSprite();
            sr.color = c;
            sr.sortingOrder = order;
            return sr;
        }

        private void ApplyDot(SpriteRenderer sr, float lx, float ly, float rad, bool awake)
        {
            Vector3 p = _controller.LogicalToWorldPublic(lx, ly);
            sr.transform.position = new Vector3(p.x, p.y, 0f);
            float worldD = 2f * rad / _controller.PixelsPerWorldUnit;
            float sx = Mathf.Max(1e-6f, sr.sprite != null ? sr.sprite.bounds.size.x : GetDefaultSprite().bounds.size.x);
            sr.transform.localScale = Vector3.one * Mathf.Max(0.03f, worldD / sx);
            var col = sr.color;
            col.a = awake ? 0.95f : 0.45f;
            sr.color = col;
        }

        private void SyncPulses()
        {
            int n = _controller.GetPulseCount();
            while (_pulseRings.Count < n)
            {
                var go = new GameObject("Pulse");
                go.transform.SetParent(_fxRoot, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.loop = true;
                lr.widthMultiplier = 0.035f;
                lr.material = new Material(Shader.Find("Sprites/Default"));
                lr.startColor = _pulseColor;
                lr.endColor = _pulseColor;
                lr.sortingOrder = 1;
                lr.useWorldSpace = true;
                _pulseRings.Add(lr);
            }

            while (_pulseRings.Count > n)
            {
                int last = _pulseRings.Count - 1;
                Destroy(_pulseRings[last].gameObject);
                _pulseRings.RemoveAt(last);
            }

            for (int i = 0; i < n; i++)
            {
                float lradius = _controller.GetPulseRadius(i);
                _controller.GetNest(out float nx, out float ny, out _, out _, out _);
                BuildCircleLine(_pulseRings[i], _controller.LogicalToWorldPublic(nx, ny), lradius / _controller.PixelsPerWorldUnit, 48);
            }
        }

        private void SyncStaticTraps()
        {
            int n = _controller.GetStaticTrapCount();
            while (_staticTrapRings.Count < n)
                _staticTrapRings.Add(CreateRingSprite(_fxRoot, "BaseTrap", ResolveMudSprite(), _mudTrapTint, -910));
            while (_staticTrapRings.Count > n)
            {
                int last = _staticTrapRings.Count - 1;
                Destroy(_staticTrapRings[last].gameObject);
                _staticTrapRings.RemoveAt(last);
            }

            for (int i = 0; i < n; i++)
            {
                var sr = _staticTrapRings[i];
                sr.sprite = ResolveMudSprite();
                sr.color = _mudTrapTint;
                _controller.GetStaticTrap(i, out float lx, out float ly, out float r);
                ApplyRing(sr, lx, ly, r);
            }
        }

        private void SyncPlayerTraps()
        {
            int n = _controller.GetPlayerTrapCount();
            while (_playerTrapRings.Count < n)
                _playerTrapRings.Add(CreateRingSprite(_fxRoot, "PlayerTrap", ResolveMudSprite(), _mudTrapTint, -920));
            while (_playerTrapRings.Count > n)
            {
                int last = _playerTrapRings.Count - 1;
                Destroy(_playerTrapRings[last].gameObject);
                _playerTrapRings.RemoveAt(last);
            }

            for (int i = 0; i < n; i++)
            {
                _controller.GetPlayerTrap(i, out float lx, out float ly, out float r, out bool slow);
                var sr = _playerTrapRings[i];
                if (slow)
                {
                    sr.sprite = ResolveMudSprite();
                    sr.color = _mudTrapTint;
                }
                else
                {
                    sr.sprite = ResolveFlySprite();
                    sr.color = _flyTrapTint;
                }

                ApplyRing(sr, lx, ly, r);
            }
        }

        private SpriteRenderer CreateRingSprite(Transform parent, string name, Sprite sprite, Color tint, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = tint;
            sr.sortingOrder = order;
            return sr;
        }

        private Sprite ResolveMudSprite() => _mudTrapSprite != null ? _mudTrapSprite : GetDefaultSprite();

        private Sprite ResolveFlySprite() => _flyTrapSprite != null ? _flyTrapSprite : GetDefaultSprite();

        private void ApplyRing(SpriteRenderer sr, float lx, float ly, float r)
        {
            Vector3 p = _controller.LogicalToWorldPublic(lx, ly);
            sr.transform.position = new Vector3(p.x, p.y, 0f);
            float worldD = 2f * r / _controller.PixelsPerWorldUnit;
            float sx = Mathf.Max(1e-6f, sr.sprite != null ? sr.sprite.bounds.size.x : GetDefaultSprite().bounds.size.x);
            sr.transform.localScale = Vector3.one * Mathf.Max(0.05f, worldD / sx);
        }

        private void SyncCones()
        {
            if (_coneMaterial == null || _signalCone == null || _repelCone == null) return;

            Transform player = _controller.PlayerVisualTransform;
            if (player == null)
            {
                var go = GameObject.FindGameObjectWithTag("Player");
                if (go != null) player = go.transform;
            }

            if (player == null) return;

            bool show = _controller.Phase == SignalRunPhase.Playing;
            bool showSignal = show && _controller.PlayerIsEmitting;
            bool showRepel = show && _controller.PlayerIsRepelling;
            _signalCone.enabled = showSignal;
            _repelCone.enabled = showRepel;

            if (!showSignal && !showRepel) return;

            _signalCone.sortingOrder = _coneSortingOrder;
            _repelCone.sortingOrder = _coneSortingOrder - 1;

            float half = _controller.Balance.Signal.ConeAngleRadians * 0.5f;
            float radiusWorld = _controller.PlayerSignalRadiusLogical / _controller.PixelsPerWorldUnit;
            Vector3 origin = player.position;
            float ang = _controller.PlayerAngleRadians;

            ApplyConeWorldTransform(_signalCone, origin, ang, radiusWorld, showSignal);
            ApplyConeWorldTransform(_repelCone, origin, ang, radiusWorld, showRepel);

            if (showSignal)
            {
                _mpbSignal.Clear();
                _mpbSignal.SetColor(BaseColorId, _signalConeBase);
                _mpbSignal.SetColor(WaveColorId, _signalConeWave);
                _mpbSignal.SetFloat(HalfAngleRadId, half);
                _signalCone.SetPropertyBlock(_mpbSignal);
            }

            if (showRepel)
            {
                _mpbRepel.Clear();
                _mpbRepel.SetColor(BaseColorId, _repelConeBase);
                _mpbRepel.SetColor(WaveColorId, _repelConeWave);
                _mpbRepel.SetFloat(HalfAngleRadId, half);
                _repelCone.SetPropertyBlock(_mpbRepel);
            }
        }

        private void ApplyConeWorldTransform(SpriteRenderer sr, Vector3 origin, float angRad, float radiusWorld, bool visible)
        {
            if (!visible) return;
            sr.transform.position = new Vector3(origin.x, origin.y, origin.z);
            sr.transform.rotation = Quaternion.Euler(0f, 0f, angRad * Mathf.Rad2Deg);
            Sprite sp = sr.sprite != null ? sr.sprite : GetDefaultSprite();
            float sx = Mathf.Max(1e-6f, sp.bounds.size.x);
            float diameterWorld = 2f * radiusWorld;
            float s = diameterWorld / sx;
            sr.transform.localScale = new Vector3(s, s, 1f);
        }

        private static void BuildCircleLine(LineRenderer lr, Vector3 center, float radiusWorld, int segments)
        {
            lr.positionCount = segments;
            float step = Mathf.PI * 2f / segments;
            for (int i = 0; i < segments; i++)
            {
                float a = i * step;
                lr.SetPosition(i, center + new Vector3(Mathf.Cos(a) * radiusWorld, Mathf.Sin(a) * radiusWorld, 0f));
            }
            lr.loop = true;
        }

        private static Sprite _cachedSprite;

        private static Sprite GetDefaultSprite()
        {
            if (_cachedSprite != null) return _cachedSprite;
            var tex = Texture2D.whiteTexture;
            _cachedSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            return _cachedSprite;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_coneMaterial != null)
                ApplyConeMaterialStaticParams();
        }
#endif
    }
}
