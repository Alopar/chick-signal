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
        [SerializeField] private SpriteRenderer _arenaBackground;
        [SerializeField] private LineRenderer _signalCone;
        [SerializeField] private LineRenderer _repelCone;
        [SerializeField] private Color _allyColor = new(0.27f, 1f, 0.53f, 0.9f);
        [SerializeField] private Color _enemyColor = new(1f, 0.26f, 0.4f, 0.9f);
        [SerializeField] private Color _nestColorCalm = new(0.27f, 1f, 0.53f, 0.85f);
        [SerializeField] private Color _nestColorAbsorb = new(0.27f, 0.67f, 1f, 0.85f);
        [SerializeField] private Color _staticTrapColor = new(0.78f, 0.35f, 0.51f, 0.35f);
        [SerializeField] private Color _playerTrapSlowColor = new(1f, 0.78f, 0.35f, 0.45f);
        [SerializeField] private Color _playerTrapAttractColor = new(0.78f, 0.47f, 1f, 0.45f);
        [SerializeField] private Color _pulseColor = new(0.27f, 0.8f, 1f, 0.5f);

        private Transform _unitsRoot;
        private Transform _fxRoot;
        private readonly List<SpriteRenderer> _allySprites = new();
        private readonly List<SpriteRenderer> _enemySprites = new();
        private readonly List<LineRenderer> _pulseRings = new();
        private readonly List<SpriteRenderer> _staticTrapRings = new();
        private readonly List<SpriteRenderer> _playerTrapRings = new();
        private SpriteRenderer _nestSprite;

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

            EnsureNestSprite();
            EnsureArenaBackground();
            EnsureConeLines();
        }

        private void EnsureNestSprite()
        {
            _nestSprite = _nestAnchor.GetComponent<SpriteRenderer>();
            if (_nestSprite == null) _nestSprite = _nestAnchor.gameObject.AddComponent<SpriteRenderer>();
            _nestSprite.sprite = GetDefaultSprite();
            _nestSprite.sortingOrder = 2;
        }

        private void EnsureArenaBackground()
        {
            if (_arenaBackground != null) return;
            var go = new GameObject("ArenaBackground");
            go.transform.SetParent(transform, false);
            go.transform.SetAsFirstSibling();
            _arenaBackground = go.AddComponent<SpriteRenderer>();
            _arenaBackground.sprite = GetDefaultSprite();
            _arenaBackground.color = new Color(0.04f, 0.04f, 0.07f, 1f);
            _arenaBackground.sortingOrder = -10;
        }

        private void EnsureConeLines()
        {
            if (_signalCone == null)
            {
                var go = new GameObject("SignalCone");
                go.transform.SetParent(transform, false);
                _signalCone = go.AddComponent<LineRenderer>();
                SetupConeLine(_signalCone, new Color(1f, 0.86f, 0.31f, 0.55f));
            }

            if (_repelCone == null)
            {
                var go = new GameObject("RepelCone");
                go.transform.SetParent(transform, false);
                _repelCone = go.AddComponent<LineRenderer>();
                SetupConeLine(_repelCone, new Color(1f, 0.55f, 0.51f, 0.5f));
            }
        }

        private static void SetupConeLine(LineRenderer lr, Color c)
        {
            lr.positionCount = 3;
            lr.widthMultiplier = 0.04f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = c;
            lr.endColor = c;
            lr.sortingOrder = 15;
            lr.useWorldSpace = true;
        }

        private void LateUpdate()
        {
            if (_controller == null) return;
            if (_controller.Balance == null) return;

            SyncArenaBackground();
            SyncNest();
            SyncUnits();
            SyncPulses();
            SyncStaticTraps();
            SyncPlayerTraps();
            SyncCones();
        }

        private void SyncArenaBackground()
        {
            float w = _controller.LogicalWidth / _controller.PixelsPerWorldUnit;
            float h = _controller.LogicalHeight / _controller.PixelsPerWorldUnit;
            float sx = Mathf.Max(1e-6f, GetDefaultSprite().bounds.size.x);
            _arenaBackground.transform.localScale = new Vector3(w / sx, h / sx, 1f);
        }

        private void SyncNest()
        {
            _controller.GetNest(out float lx, out float ly, out float r, out bool absorbing, out int level);
            Vector3 p = _controller.LogicalToWorldPublic(lx, ly);
            _nestAnchor.position = new Vector3(p.x, p.y, 0f);
            float worldD = 2f * r / _controller.PixelsPerWorldUnit;
            float sx = Mathf.Max(1e-6f, GetDefaultSprite().bounds.size.x);
            _nestAnchor.localScale = Vector3.one * Mathf.Max(0.05f, worldD / sx);
            _nestSprite.color = absorbing ? _nestColorAbsorb : _nestColorCalm;
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
            while (_staticTrapRings.Count < n) _staticTrapRings.Add(CreateRingSprite(_fxRoot, "BaseTrap", _staticTrapColor, 0));
            while (_staticTrapRings.Count > n)
            {
                int last = _staticTrapRings.Count - 1;
                Destroy(_staticTrapRings[last].gameObject);
                _staticTrapRings.RemoveAt(last);
            }

            for (int i = 0; i < n; i++)
            {
                _controller.GetStaticTrap(i, out float lx, out float ly, out float r);
                ApplyRing(_staticTrapRings[i], lx, ly, r);
            }
        }

        private void SyncPlayerTraps()
        {
            int n = _controller.GetPlayerTrapCount();
            while (_playerTrapRings.Count < n) _playerTrapRings.Add(CreateRingSprite(_fxRoot, "PlayerTrap", _playerTrapSlowColor, 3));
            while (_playerTrapRings.Count > n)
            {
                int last = _playerTrapRings.Count - 1;
                Destroy(_playerTrapRings[last].gameObject);
                _playerTrapRings.RemoveAt(last);
            }

            for (int i = 0; i < n; i++)
            {
                _controller.GetPlayerTrap(i, out float lx, out float ly, out float r, out bool slow);
                _playerTrapRings[i].color = slow ? _playerTrapSlowColor : _playerTrapAttractColor;
                ApplyRing(_playerTrapRings[i], lx, ly, r);
            }
        }

        private SpriteRenderer CreateRingSprite(Transform parent, string name, Color c, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetDefaultSprite();
            sr.color = c;
            sr.sortingOrder = order;
            return sr;
        }

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
            Transform player = _controller.PlayerVisualTransform;
            if (player == null)
            {
                var go = GameObject.FindGameObjectWithTag("Player");
                if (go != null) player = go.transform;
            }

            if (player == null) return;

            bool show = _controller.Phase == SignalRunPhase.Playing;
            _signalCone.enabled = show && _controller.PlayerIsEmitting;
            _repelCone.enabled = show && _controller.PlayerIsRepelling;

            if (!_signalCone.enabled && !_repelCone.enabled) return;

            float half = _controller.Balance.Signal.ConeAngleRadians * 0.5f;
            float radiusWorld = _controller.PlayerSignalRadiusLogical / _controller.PixelsPerWorldUnit;
            Vector3 origin = player.position;
            float ang = _controller.PlayerAngleRadians;
            Vector3 forward = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f);
            Vector3 left = Quaternion.AngleAxis(-half * Mathf.Rad2Deg, Vector3.forward) * forward * radiusWorld;
            Vector3 right = Quaternion.AngleAxis(half * Mathf.Rad2Deg, Vector3.forward) * forward * radiusWorld;

            if (_signalCone.enabled)
            {
                _signalCone.SetPosition(0, origin);
                _signalCone.SetPosition(1, origin + left);
                _signalCone.SetPosition(2, origin + right);
            }

            if (_repelCone.enabled)
            {
                _repelCone.SetPosition(0, origin);
                _repelCone.SetPosition(1, origin + left);
                _repelCone.SetPosition(2, origin + right);
            }
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
    }
}
