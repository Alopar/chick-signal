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
        private static readonly int FeedingHash = Animator.StringToHash("Feeding");
        private const float NpcMoveThresholdSq = 0.000001f;

        [SerializeField] private SignalGameController _controller;
        [SerializeField] private Transform _nestAnchor;
        [SerializeField] private GameObject _nestVisualPrefab;
        [SerializeField] private GameObject _caterpillarVisualPrefab;
        [SerializeField] private GameObject _spiderVisualPrefab;
        [SerializeField] private SpriteRenderer _signalCone;
        [SerializeField] private SpriteRenderer _repelCone;
        [Tooltip("Все параметры шейдера конуса — на этом компоненте (или будет добавлен автоматически).")]
        [SerializeField] private SignalConeVisualSettings _coneVisualSettings;
        [SerializeField] private Sprite _mudTrapSprite;
        [SerializeField] private Sprite _flyTrapSprite;
        [SerializeField] private Color _mudTrapTint = new(1f, 1f, 1f, 0.88f);
        [SerializeField] private Color _flyTrapTint = new(1f, 1f, 1f, 0.88f);
        [SerializeField] private Color _pulseColor = new(0.27f, 0.8f, 1f, 0.5f);
        [Header("Nest Chick Growth")]
        [SerializeField] private float _chickBaseScale = 1f;
        [SerializeField] private float _chickGrowthPerNestRadiusUnit = 0.01f;
        [Tooltip("100% = текущий темп роста. Больше — птенец растет визуально быстрее.")]
        [SerializeField] private float _chickGrowthPercent = 100f;
        [Header("Nest Debug Rings")]
        [SerializeField] private bool _showNestDebugRings;
        [SerializeField] private Color _realNestRadiusColor = new(1f, 0.25f, 0.25f, 0.95f);
        [SerializeField] private Color _visualChickRadiusColor = new(0.2f, 0.95f, 1f, 0.95f);
        [SerializeField] private float _debugRingWidth = 0.03f;

        private Transform _unitsRoot;
        private Transform _fxRoot;
        private readonly List<NpcVisual> _allyVisuals = new();
        private readonly List<NpcVisual> _enemyVisuals = new();
        private readonly List<LineRenderer> _pulseRings = new();
        private readonly List<SpriteRenderer> _staticTrapRings = new();
        private readonly List<SpriteRenderer> _playerTrapRings = new();
        private SignalNestVisual _nestVisual;
        private Material _coneMaterial;
        private MaterialPropertyBlock _mpbSignal;
        private MaterialPropertyBlock _mpbRepel;
        private LineRenderer _realNestRadiusRing;
        private LineRenderer _visualChickRadiusRing;

        private void Awake()
        {
            if (_controller == null) _controller = GetComponent<SignalGameController>();
            EnsureConeVisualSettings();
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

        private void EnsureConeVisualSettings()
        {
            if (_coneVisualSettings == null)
                _coneVisualSettings = GetComponent<SignalConeVisualSettings>();
            if (_coneVisualSettings == null)
                _coneVisualSettings = gameObject.AddComponent<SignalConeVisualSettings>();
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
            int order = _coneVisualSettings != null ? _coneVisualSettings.ConeSortingOrder : -8;
            _signalCone.sortingOrder = order;
            _repelCone.sortingOrder = order - 1;
        }

        private void ApplyConeMaterialStaticParams()
        {
            if (_coneMaterial == null || _coneVisualSettings == null) return;
            _coneVisualSettings.ApplyStaticToMaterial(_coneMaterial);
        }

        private void SetupConeSpriteRenderer(SpriteRenderer sr)
        {
            sr.sprite = _coneVisualSettings != null && _coneVisualSettings.ConeSprite != null
                ? _coneVisualSettings.ConeSprite
                : GetDefaultSprite();
            sr.color = Color.white;
            sr.sortingOrder = _coneVisualSettings != null ? _coneVisualSettings.ConeSortingOrder : -8;
        }

        private void LateUpdate()
        {
            if (_controller == null) return;
            if (_controller.Balance == null) return;

            if (_coneMaterial != null && _coneVisualSettings != null)
                _coneVisualSettings.ApplyStaticToMaterial(_coneMaterial);

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

            _controller.GetNest(out float lx, out float ly, out float nestRadius, out bool absorbing, out _);
            Vector3 p = _controller.LogicalToWorldPublic(lx, ly);
            _nestAnchor.position = new Vector3(p.x, p.y, 0f);
            _nestVisual.SetAbsorbing(absorbing);

            float initialNestRadius = _controller.Balance != null
                ? Mathf.Max(1f, _controller.Balance.Nest.Radius)
                : Mathf.Max(1f, nestRadius);
            float growth = Mathf.Max(0f, nestRadius - initialNestRadius);
            float growthMultiplier = Mathf.Max(0f, _chickGrowthPercent) / 100f;
            float chickScale = _chickBaseScale + growth * _chickGrowthPerNestRadiusUnit * growthMultiplier;
            _nestVisual.SetChickScale(chickScale);
            SyncNestDebugRings(_nestAnchor.position, nestRadius, initialNestRadius, chickScale);
        }

        private void SyncNestDebugRings(Vector3 centerWorld, float realNestRadiusLogical, float initialNestRadiusLogical, float chickScale)
        {
            if (!_showNestDebugRings)
            {
                SetDebugRingEnabled(_realNestRadiusRing, false);
                SetDebugRingEnabled(_visualChickRadiusRing, false);
                return;
            }

            _realNestRadiusRing ??= CreateDebugRing("NestRealRadiusRing", _realNestRadiusColor);
            _visualChickRadiusRing ??= CreateDebugRing("NestVisualRadiusRing", _visualChickRadiusColor);

            float realRadiusWorld = realNestRadiusLogical / Mathf.Max(1e-6f, _controller.PixelsPerWorldUnit);
            float visualScaleRatio = chickScale / Mathf.Max(1e-6f, _chickBaseScale);
            float visualNestRadiusLogical = initialNestRadiusLogical * visualScaleRatio;
            float visualRadiusWorld = visualNestRadiusLogical / Mathf.Max(1e-6f, _controller.PixelsPerWorldUnit);

            BuildCircleLine(_realNestRadiusRing, centerWorld, realRadiusWorld, 64);
            BuildCircleLine(_visualChickRadiusRing, centerWorld, visualRadiusWorld, 64);
            SetDebugRingEnabled(_realNestRadiusRing, true);
            SetDebugRingEnabled(_visualChickRadiusRing, true);
        }

        private LineRenderer CreateDebugRing(string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_fxRoot != null ? _fxRoot : transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.loop = true;
            lr.positionCount = 0;
            lr.widthMultiplier = Mathf.Max(0.001f, _debugRingWidth);
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = color;
            lr.endColor = color;
            lr.sortingOrder = 10;
            lr.useWorldSpace = true;
            return lr;
        }

        private static void SetDebugRingEnabled(LineRenderer ring, bool enabled)
        {
            if (ring != null)
                ring.enabled = enabled;
        }

        private void SyncUnits()
        {
            _controller.GetNest(out _, out _, out _, out bool nestFeeding, out _);

            int ac = _controller.GetAllyCount();
            while (_allyVisuals.Count < ac) _allyVisuals.Add(CreateNpcVisual(_unitsRoot, SignalNpcKind.Green, 4, "Ally"));
            while (_allyVisuals.Count > ac)
            {
                int last = _allyVisuals.Count - 1;
                Destroy(_allyVisuals[last].Root.gameObject);
                _allyVisuals.RemoveAt(last);
            }

            for (int i = 0; i < ac; i++)
            {
                _controller.GetAlly(i, out float lx, out float ly, out float rad, out float mt);
                ApplyNpcVisual(_allyVisuals[i], lx, ly, nestFeeding);
            }

            int ec = _controller.GetEnemyCount();
            while (_enemyVisuals.Count < ec) _enemyVisuals.Add(CreateNpcVisual(_unitsRoot, SignalNpcKind.Red, 5, "Enemy"));
            while (_enemyVisuals.Count > ec)
            {
                int last = _enemyVisuals.Count - 1;
                Destroy(_enemyVisuals[last].Root.gameObject);
                _enemyVisuals.RemoveAt(last);
            }

            for (int i = 0; i < ec; i++)
            {
                _controller.GetEnemy(i, out float lx, out float ly, out float rad, out float mt, out SignalNpcKind kind);
                EnsureEnemyVisualKind(i, kind, 5);
                ApplyNpcVisual(_enemyVisuals[i], lx, ly, nestFeeding);
            }
        }

        private void EnsureEnemyVisualKind(int index, SignalNpcKind kind, int sortingOrder)
        {
            var current = _enemyVisuals[index];
            if (current.Kind == kind) return;
            Destroy(current.Root.gameObject);
            _enemyVisuals[index] = CreateNpcVisual(_unitsRoot, kind, sortingOrder, "Enemy");
        }

        private NpcVisual CreateNpcVisual(Transform parent, SignalNpcKind kind, int sortingOrder, string fallbackName)
        {
            GameObject prefab = kind == SignalNpcKind.Red ? _spiderVisualPrefab : _caterpillarVisualPrefab;
            GameObject instance;
            if (prefab != null)
            {
                instance = Instantiate(prefab, parent, false);
                instance.name = prefab.name;
            }
            else
            {
                instance = new GameObject(fallbackName);
                instance.transform.SetParent(parent, false);
                var fallbackRenderer = instance.AddComponent<SpriteRenderer>();
                fallbackRenderer.sprite = GetDefaultSprite();
            }

            var renderer = instance.GetComponentInChildren<SpriteRenderer>();
            if (renderer == null) renderer = instance.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = sortingOrder;

            return new NpcVisual
            {
                Kind = kind,
                Root = instance.transform,
                SpriteRenderer = renderer,
                Animator = instance.GetComponentInChildren<Animator>(),
            };
        }

        private void ApplyNpcVisual(NpcVisual visual, float lx, float ly, bool feeding)
        {
            Vector3 p = _controller.LogicalToWorldPublic(lx, ly);
            Vector3 targetPos = new Vector3(p.x, p.y, 0f);
            Vector3 delta = visual.Initialized ? targetPos - visual.LastWorldPosition : Vector3.zero;
            bool moving = delta.sqrMagnitude > NpcMoveThresholdSq;
            if (moving && Mathf.Abs(delta.x) > 1e-6f)
                visual.SpriteRenderer.flipX = delta.x > 0f;

            visual.Root.position = targetPos;
            if (visual.Animator != null)
            {
                visual.Animator.SetBool(FeedingHash, feeding);
                // Если юнит стоит на месте, анимация должна быть поставлена на паузу.
                visual.Animator.speed = moving ? 1f : 0f;
            }

            visual.LastWorldPosition = targetPos;
            visual.Initialized = true;
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
            if (_coneMaterial == null || _signalCone == null || _repelCone == null || _coneVisualSettings == null) return;

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

            int order = _coneVisualSettings.ConeSortingOrder;
            _signalCone.sortingOrder = order;
            _repelCone.sortingOrder = order - 1;

            float half = _controller.Balance.Signal.ConeAngleRadians * 0.5f;
            float radiusWorld = _controller.PlayerSignalRadiusLogical / _controller.PixelsPerWorldUnit;
            Vector3 origin = player.position;
            float ang = _controller.PlayerAngleRadians;

            ApplyConeWorldTransform(_signalCone, origin, ang, radiusWorld, showSignal);
            ApplyConeWorldTransform(_repelCone, origin, ang, radiusWorld, showRepel);

            if (showSignal)
            {
                _mpbSignal.Clear();
                _mpbSignal.SetColor(SignalConeVisualShaders.BaseColorId, _coneVisualSettings.SignalConeBase);
                _mpbSignal.SetColor(SignalConeVisualShaders.WaveColorId, _coneVisualSettings.SignalConeWave);
                _mpbSignal.SetFloat(SignalConeVisualShaders.HalfAngleRadId, half);
                _signalCone.SetPropertyBlock(_mpbSignal);
            }

            if (showRepel)
            {
                _mpbRepel.Clear();
                _mpbRepel.SetColor(SignalConeVisualShaders.BaseColorId, _coneVisualSettings.RepelConeBase);
                _mpbRepel.SetColor(SignalConeVisualShaders.WaveColorId, _coneVisualSettings.RepelConeWave);
                _mpbRepel.SetFloat(SignalConeVisualShaders.HalfAngleRadId, half);
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

        private sealed class NpcVisual
        {
            public SignalNpcKind Kind;
            public Transform Root;
            public SpriteRenderer SpriteRenderer;
            public Animator Animator;
            public Vector3 LastWorldPosition;
            public bool Initialized;
        }

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
            EnsureConeVisualSettings();
            if (_coneMaterial != null)
                ApplyConeMaterialStaticParams();
        }
#endif
    }
}
