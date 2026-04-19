using System;
using System.Collections.Generic;
using LudumDare.Template.Gameplay.Player;
using LudumDare.Template.Input;
using LudumDare.Template.Managers;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LudumDare.Template.Gameplay.Signal
{
    public enum SignalNpcKind
    {
        Red,
        Green,
    }

    public enum SignalRunPhase
    {
        Playing,
        EvolutionPick,
        Dead,
        Win,
    }

    public enum SignalEvolutionBonus
    {
        Heal,
        Trap,
        Purge,
    }

    /// <summary>
    /// Логика SIGNAL из migration-data/signal_game.html (без частиц и визуала).
    /// Координаты симуляции: ось Y вниз, как в canvas (0…H).
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class SignalGameController : MonoBehaviour
    {
        private static readonly int VisualStateHash = Animator.StringToHash("VisualState");

        [SerializeField] private SignalGameBalanceSO _balance;
        [SerializeField] private InputReader _inputReader;
        [SerializeField] private Camera _camera;
        [SerializeField] private Transform _playerTransform;
        [SerializeField] private Behaviour[] _disableWhileSignalRuns;
        [SerializeField] private SignalHudEventChannelSO _hudChannel;
        [SerializeField] private SignalEvolutionEventChannelSO _evolutionModalChannel;

        [Header("Camera")]
        [Tooltip("Время сглаживания следования камеры за игроком (SmoothDamp).")]
        [SerializeField] private float _cameraFollowSmoothTime = 0.18f;
        [Tooltip("Макс. скорость догонания камеры (0 = без ограничения).")]
        [SerializeField] private float _cameraFollowMaxSpeed = 80f;
        [Tooltip("Насколько далеко в сторону курсора может сместиться «якорь» камеры в мировых единицах (как в Nuclear Throne).")]
        [SerializeField] private float _cameraMouseBiasMaxWorld = 0.42f;
        [Tooltip("При какой дистанции игрок→курсор в мире смещение почти достигает максимума.")]
        [SerializeField] private float _cameraMouseBiasRampWorld = 3.2f;

        public SignalHudEventChannelSO HudChannel => _hudChannel;
        public SignalEvolutionEventChannelSO EvolutionChannel => _evolutionModalChannel;

        /// <summary>Вызывается UI после подписки на канал, чтобы не терять первый снимок.</summary>
        public void RefreshHud() => PushHud();

        public SignalRunPhase Phase => _phase;
        public float PixelsPerWorldUnit => _pixelsPerUnit;
        public float LogicalWidth => _w;
        public float LogicalHeight => _h;
        public SignalGameBalanceSO Balance => _balance;

        public Vector3 LogicalToWorldPublic(float lx, float ly) => LogicalToWorld(lx, ly);

        public int GetAllyCount() => _allies.Count;
        public void GetAlly(int i, out float lx, out float ly, out float radius, out float moveTimer)
        {
            var a = _allies[i];
            lx = a.X;
            ly = a.Y;
            radius = a.Radius;
            moveTimer = a.MoveTimer;
        }

        public int GetEnemyCount() => _enemies.Count;
        public void GetEnemy(int i, out float lx, out float ly, out float radius, out float moveTimer, out SignalNpcKind kind)
        {
            var e = _enemies[i];
            lx = e.X;
            ly = e.Y;
            radius = e.Radius;
            moveTimer = e.MoveTimer;
            kind = e.Kind;
        }

        public void GetNest(out float lx, out float ly, out float radius, out bool absorbing, out int level)
        {
            lx = _nest.X;
            ly = _nest.Y;
            radius = _nest.Radius;
            absorbing = _nest.Absorbing;
            level = _nest.Level;
        }

        public int GetPulseCount() => _pulses.Count;
        public float GetPulseRadius(int i) => _pulses[i].R;

        public float GetNestPulseMaxRadiusLogical() => NestPulseMaxR();

        public int GetStaticTrapCount() => _trapZones.Count;
        public void GetStaticTrap(int i, out float lx, out float ly, out float r)
        {
            var z = _trapZones[i];
            lx = z.X;
            ly = z.Y;
            r = z.R;
        }

        public int GetPlayerTrapCount() => _playerTraps.Count;
        public void GetPlayerTrap(int i, out float lx, out float ly, out float r, out bool isSlow)
        {
            var t = _playerTraps[i];
            lx = t.X;
            ly = t.Y;
            r = t.R;
            isSlow = t.Kind == PlayerTrapKind.Slow;
        }

        public float PlayerAngleRadians => _player.Angle;
        public bool PlayerIsEmitting => _player.IsEmitting;
        public bool PlayerIsRepelling => _player.IsRepelling;
        public float PlayerSignalRadiusLogical => _player.SignalRadius;
        public Transform PlayerVisualTransform => _playerTransform;

        private float _w;
        private float _h;
        private float _pixelsPerUnit = 100f;

        private SignalRunPhase _phase = SignalRunPhase.Playing;

        private PlayerSim _player;
        private NestSim _nest;
        private readonly List<AllySim> _allies = new();
        private readonly List<EnemySim> _enemies = new();
        private readonly List<PulseSim> _pulses = new();
        private readonly List<TrapZoneSim> _trapZones = new();
        private readonly List<PlayerTrapSim> _playerTraps = new();

        private TrapStock _trapSlow;
        private TrapStock _trapAttract;

        private int _waveDisplayIndex = 1;
        private int _waveNumber = 1;
        private float _waveElapsed;
        private readonly Dictionary<string, List<float>> _waveSpawnTimes = new();
        private readonly Dictionary<string, int> _waveSpawnNext = new();

        private float _gameTime;
        private Vector3 _cameraFollowVelocity;

        private void Awake()
        {
            if (_balance == null)
            {
                _balance = ScriptableObject.CreateInstance<SignalGameBalanceSO>();
                SignalBalanceDefaults.ApplyHtmlDefaults(_balance);
            }

            if (_hudChannel == null)
                _hudChannel = ScriptableObject.CreateInstance<SignalHudEventChannelSO>();
            if (_evolutionModalChannel == null)
                _evolutionModalChannel = ScriptableObject.CreateInstance<SignalEvolutionEventChannelSO>();

            if (_inputReader == null)
                _inputReader = Resources.Load<InputReader>("InputReader");

            if (_camera == null) _camera = Camera.main;
            if (_playerTransform == null)
            {
                var go = GameObject.FindGameObjectWithTag("Player");
                if (go != null) _playerTransform = go.transform;
            }

            if (_disableWhileSignalRuns == null || _disableWhileSignalRuns.Length == 0)
            {
                var pc = FindAnyObjectByType<PlayerController>();
                if (pc != null) _disableWhileSignalRuns = new[] { pc };
            }

            if (_disableWhileSignalRuns != null)
            {
                for (int i = 0; i < _disableWhileSignalRuns.Length; i++)
                {
                    if (_disableWhileSignalRuns[i] != null) _disableWhileSignalRuns[i].enabled = false;
                }
            }

            if (_playerTransform != null)
            {
                var rb = _playerTransform.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.bodyType = RigidbodyType2D.Kinematic;
                    rb.linearVelocity = Vector2.zero;
                }
            }

            if (_camera != null && _balance != null)
            {
                _camera.orthographicSize = _balance.ReferenceCanvasHeight /
                    (2f * Mathf.Max(0.0001f, _balance.LogicalPixelsPerWorldUnit));
            }
        }

        private void OnEnable()
        {
            EnsureBalance();
            if (_inputReader != null)
            {
                _inputReader.EnablePlayer();
                _inputReader.OnJump += HandleDash;
                _inputReader.OnTrapSlow += HandleTrapSlow;
                _inputReader.OnTrapAttract += HandleTrapAttract;
            }

            InitGame();
            _phase = SignalRunPhase.Playing;
        }

        private void OnDisable()
        {
            if (_inputReader != null)
            {
                _inputReader.OnJump -= HandleDash;
                _inputReader.OnTrapSlow -= HandleTrapSlow;
                _inputReader.OnTrapAttract -= HandleTrapAttract;
                _inputReader.DisableAll();
            }
        }

        private void EnsureBalance()
        {
            if (_balance == null)
            {
                Debug.LogError("[Signal] Assign SignalGameBalanceSO.");
                return;
            }

            if (_balance.Spawn.Waves == null || _balance.Spawn.Waves.Count == 0 || _balance.Player.Speed < 1f)
            {
                SignalBalanceDefaults.ApplyHtmlDefaults(_balance);
            }
        }

        private void InitGame()
        {
            EnsureBalance();
            if (_balance == null) return;

            _pixelsPerUnit = Mathf.Max(0.0001f, _balance.LogicalPixelsPerWorldUnit);
            _w = _balance.ReferenceCanvasWidth;
            _h = _balance.ReferenceCanvasHeight;

            var p = _balance.Player;
            var n = _balance.Nest;
            float dxFromNest = p.StartDx - n.StartDx;
            _player = new PlayerSim
            {
                X = _w * 0.5f + dxFromNest,
                Y = _h * 0.5f,
                Speed = p.Speed,
                Hp = p.Hp,
                MaxHp = p.MaxHp,
                SignalPower = p.SignalPower,
                SignalRadius = p.SignalRadius,
                Radius = p.Radius,
                SignalJam = 0f,
                MoveSlowTimer = 0f,
                DashTimeLeft = 0f,
                DashCooldownLeft = 0f,
                DashDirX = 1f,
                DashDirY = 0f,
            };

            _nest = new NestSim
            {
                X = _w * 0.5f,
                Y = _h * 0.5f,
                Level = n.InitialLevel,
                Hp = n.Hp,
                MaxHp = n.MaxHp,
                Charge = 0f,
                ChargeMax = n.InitialChargeMax,
                Satiety = 0f,
                Absorbing = false,
                AbsorptionTimeLeft = 0f,
                AbsorptionChargeStart = 0f,
                HealRadius = n.HealRadius,
                SignalPower = n.SignalPower,
                Radius = n.Radius,
                AoeTimer = 0f,
                PulseTimer = 0f,
            };

            _allies.Clear();
            _enemies.Clear();
            _pulses.Clear();
            _playerTraps.Clear();

            _waveDisplayIndex = 1;
            _waveNumber = 1;
            _waveElapsed = 0f;
            _waveSpawnTimes.Clear();
            _waveSpawnNext.Clear();
            _gameTime = 0f;

            RebuildEnemySpawnSchedule();
            var sp = _balance.Spawn;
            for (int i = 0; i < sp.StartingAllies; i++) SpawnAlly();

            GenerateTrapZones();

            int maxC = _balance.Traps.PlayerTrapMaxCharges;
            _trapSlow = new TrapStock { Charges = maxC, CooldownLeft = 0f };
            _trapAttract = new TrapStock { Charges = maxC, CooldownLeft = 0f };

            PushHud();
            if (_playerTransform != null)
            {
                Vector3 pw = LogicalToWorld(_player.X, _player.Y);
                _playerTransform.position = new Vector3(pw.x, pw.y, _playerTransform.position.z);
                _playerTransform.rotation = Quaternion.identity;
            }

            SnapCameraToPlayerWorld();
        }

        private void FixedUpdate()
        {
            if (_balance == null || _phase != SignalRunPhase.Playing) return;

            float dt = Mathf.Min(Time.fixedDeltaTime, _balance.LoopMaxDeltaTime);
            Step(dt);
            PushHud();
            SyncTransforms();
        }

        private void Update()
        {
            if (_balance == null || _inputReader == null) return;
            if (_phase != SignalRunPhase.Playing) return;

            bool jammed = _player.SignalJam > 0f;
            _player.IsEmitting = _inputReader.IsAttackHeld && !jammed;
            _player.IsRepelling = _inputReader.IsRepelHeld && !jammed;
        }

        private void LateUpdate()
        {
            if (_balance == null || _phase != SignalRunPhase.Playing) return;

            UpdateCameraFollow();
            if (_inputReader == null) return;

            Vector2 mouseLogical = GetMouseLogicalPosition();
            _player.Angle = LogicalAngleFromTo(_player.X, _player.Y, mouseLogical.x, mouseLogical.y);
            SyncPlayerVisual();
        }

        /// <summary>
        /// В режиме Signal <see cref="PlayerController"/> отключён — анимация и флип спрайта задаются здесь.
        /// Поворот корня не используем (угол сигнала — только для конусов через <see cref="PlayerAngleRadians"/>).
        /// </summary>
        private void SyncPlayerVisual()
        {
            if (_playerTransform == null) return;

            var playerController = _playerTransform.GetComponent<PlayerController>();
            if (playerController != null && playerController.enabled)
                return;

            const float moveThreshSq = 0.05f * 0.05f;

            var anim = _playerTransform.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                bool moving = _player.DashTimeLeft > 0f;
                if (!moving && _inputReader != null)
                    moving = _inputReader.MoveValue.sqrMagnitude > moveThreshSq;
                int state = BertVisualState.Compute(moving, _player.IsEmitting, _player.IsRepelling);
                anim.SetInteger(VisualStateHash, state);
            }

            var sr = _playerTransform.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                Vector3 mouse = GetMouseWorldPositionXY();
                sr.flipX = mouse.x > _playerTransform.position.x;
            }
        }

        private Vector2 GetMouseLogicalPosition()
        {
            if (_camera == null || Mouse.current == null)
            {
                return new Vector2(_player.X + 1f, _player.Y);
            }

            Vector3 sp = Mouse.current.position.ReadValue();
            Vector3 w = _camera.ScreenToWorldPoint(new Vector3(sp.x, sp.y, -_camera.transform.position.z));
            float lx = w.x * _pixelsPerUnit + _w * 0.5f;
            float ly = -w.y * _pixelsPerUnit + _h * 0.5f;
            return new Vector2(lx, ly);
        }

        private void SnapCameraToPlayerWorld()
        {
            if (_camera == null || _playerTransform == null) return;
            Vector3 c = _camera.transform.position;
            Vector3 target = GetCameraFollowTargetWorld(c.z);
            _camera.transform.position = target;
            _cameraFollowVelocity = Vector3.zero;
        }

        private Vector3 GetMouseWorldPositionXY()
        {
            if (_camera == null || Mouse.current == null)
            {
                return _playerTransform != null ? _playerTransform.position : Vector3.zero;
            }

            Vector3 sp = Mouse.current.position.ReadValue();
            Vector3 w = _camera.ScreenToWorldPoint(new Vector3(sp.x, sp.y, -_camera.transform.position.z));
            return new Vector3(w.x, w.y, 0f);
        }

        /// <summary>
        /// Цель камеры: между игроком и курсором; чем дальше курсор от игрока, тем сильнее сдвиг (до лимита).
        /// </summary>
        private Vector3 GetCameraFollowTargetWorld(float z)
        {
            if (_playerTransform == null) return new Vector3(0f, 0f, z);
            Vector3 p = _playerTransform.position;
            if (_cameraMouseBiasMaxWorld <= 0f) return new Vector3(p.x, p.y, z);

            Vector3 mouse = GetMouseWorldPositionXY();
            float dx = mouse.x - p.x;
            float dy = mouse.y - p.y;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            if (dist < 1e-5f) return new Vector3(p.x, p.y, z);

            float ramp = Mathf.Max(0.01f, _cameraMouseBiasRampWorld);
            float t = Mathf.Clamp01(dist / ramp);
            t = t * t * (3f - 2f * t);
            float off = t * _cameraMouseBiasMaxWorld;
            float nx = dx / dist;
            float ny = dy / dist;
            return new Vector3(p.x + nx * off, p.y + ny * off, z);
        }

        private void UpdateCameraFollow()
        {
            if (_camera == null || _playerTransform == null) return;
            Vector3 c = _camera.transform.position;
            float smooth = Mathf.Max(0.0001f, _cameraFollowSmoothTime);
            float maxSpd = _cameraFollowMaxSpeed > 0f ? _cameraFollowMaxSpeed : float.PositiveInfinity;
            Vector3 target = GetCameraFollowTargetWorld(c.z);
            _camera.transform.position = Vector3.SmoothDamp(c, target, ref _cameraFollowVelocity, smooth, maxSpd, Time.deltaTime);
        }

        /// <summary>
        /// Радиус появления юнитов вокруг игрока — чуть дальше видимой области камеры.
        /// </summary>
        private float GetSpawnRingRadiusLogical()
        {
            float margin = Mathf.Max(1f, _balance.Spawn.EdgeMargin);
            if (_camera != null)
            {
                float halfH = _camera.orthographicSize * _pixelsPerUnit;
                float halfW = halfH * _camera.aspect;
                return Mathf.Sqrt(halfW * halfW + halfH * halfH) + margin * 2f;
            }

            return Mathf.Max(_w, _h) * 0.55f + margin;
        }

        private void HandleDash()
        {
            if (_phase != SignalRunPhase.Playing || _balance == null) return;
            if (_player.DashTimeLeft > 0f || _player.DashCooldownLeft > 0f) return;

            Vector2 m = GetMouseLogicalPosition();
            float dlx = m.x - _player.X;
            float dly = m.y - _player.Y;
            float len = Mathf.Sqrt(dlx * dlx + dly * dly);
            if (len > 1e-6f)
            {
                _player.DashDirX = dlx / len;
                _player.DashDirY = dly / len;
            }
            else
            {
                _player.DashDirX = 1f;
                _player.DashDirY = 0f;
            }

            _player.DashTimeLeft = _balance.Player.DashDuration;
        }

        private void HandleTrapSlow() => TryPlacePlayerTrap(PlayerTrapKind.Slow);

        private void HandleTrapAttract() => TryPlacePlayerTrap(PlayerTrapKind.Attract);

        public void ApplyEvolutionBonus(SignalEvolutionBonus bonus)
        {
            if (_phase != SignalRunPhase.EvolutionPick) return;

            var B = _balance;
            switch (bonus)
            {
                case SignalEvolutionBonus.Heal:
                    _nest.MaxHp += 10f;
                    _nest.Hp = _nest.MaxHp;
                    break;
                case SignalEvolutionBonus.Trap:
                    AddBonusTrapZone();
                    break;
                case SignalEvolutionBonus.Purge:
                    _enemies.Clear();
                    break;
            }

            _evolutionModalChannel?.Raise(false);
            _phase = SignalRunPhase.Playing;
        }

        private void Step(float dt)
        {
            var S = _balance;
            _gameTime += dt;

            for (int i = 0; i < _playerTraps.Count; i++)
            {
                var pt = _playerTraps[i];
                pt.TimeLeft -= dt;
                _playerTraps[i] = pt;
            }
            _playerTraps.RemoveAll(t => t.TimeLeft <= 0f);

            StepTrapStock(ref _trapSlow, S.Traps.PlayerTrapMaxCharges, S.Traps.PlayerTrapRechargeSlow, dt);
            StepTrapStock(ref _trapAttract, S.Traps.PlayerTrapMaxCharges, S.Traps.PlayerTrapRechargeAttract, dt);

            UpdateWaveEnemySpawns(dt);

            var p = S.Player;
            if (_player.DashTimeLeft > 0f)
            {
                float dSpd = p.DashDistance / Mathf.Max(1e-6f, p.DashDuration);
                _player.X += _player.DashDirX * dSpd * dt;
                _player.Y += _player.DashDirY * dSpd * dt;
                _player.DashTimeLeft -= dt;
                if (_player.DashTimeLeft <= 0f)
                {
                    _player.DashTimeLeft = 0f;
                    _player.DashCooldownLeft = p.DashCooldown;
                }
            }
            else
            {
                if (_player.DashCooldownLeft > 0f) _player.DashCooldownLeft -= dt;
                Vector2 move = _inputReader != null ? _inputReader.MoveValue : Vector2.zero;
                float dx = move.x;
                float dy = move.y;
                float len = Mathf.Sqrt(dx * dx + dy * dy);
                if (len > 1e-6f)
                {
                    dx /= len;
                    dy /= len;
                    float spd = _player.Speed * (_player.MoveSlowTimer > 0f ? S.Debuff.SlowMult : 1f);
                    _player.X += dx * spd * dt;
                    // Логическая ось Y вниз (как canvas); ввод «вверх» даёт +move.y → уменьшаем ly.
                    _player.Y -= dy * spd * dt;
                }
            }

            if (_player.SignalJam > 0f) _player.SignalJam -= dt;
            if (_player.MoveSlowTimer > 0f) _player.MoveSlowTimer -= dt;

            float dNest = Dist(_player.X, _player.Y, _nest.X, _nest.Y);
            if (dNest < _nest.HealRadius)
            {
                float proximity = 1f - dNest / Mathf.Max(1e-6f, _nest.HealRadius);
                float healRate = S.Nest.HealBase + proximity * S.Nest.HealProximityBonus;
                _player.Hp = Mathf.Min(_player.MaxHp, _player.Hp + healRate * dt);
            }

            _nest.PulseTimer += dt;
            if (_nest.PulseTimer >= S.Nest.PulseInterval)
            {
                _nest.PulseTimer = 0f;
                _pulses.Add(new PulseSim { R = _nest.Radius, Speed = S.Nest.PulseExpandSpeed });
            }

            float pulseMax = NestPulseMaxR();
            for (int i = _pulses.Count - 1; i >= 0; i--)
            {
                var pulse = _pulses[i];
                float prevR = pulse.R;
                pulse.R += pulse.Speed * dt;

                for (int a = 0; a < _allies.Count; a++)
                {
                    var al = _allies[a];
                    float d = Dist(al.X, al.Y, _nest.X, _nest.Y);
                    if (d >= prevR && d < pulse.R)
                    {
                        al.MoveTimer = S.Signal.NpcWakeDuration;
                        _allies[a] = al;
                    }
                }

                for (int e = 0; e < _enemies.Count; e++)
                {
                    var en = _enemies[e];
                    float d = Dist(en.X, en.Y, _nest.X, _nest.Y);
                    if (d >= prevR && d < pulse.R)
                    {
                        en.MoveTimer = S.Signal.NpcWakeDuration;
                        _enemies[e] = en;
                    }
                }

                if (pulse.R >= pulseMax) _pulses.RemoveAt(i);
                else _pulses[i] = pulse;
            }

            if (_nest.Level >= S.Nest.AoeUnlockLevel)
            {
                _nest.AoeTimer += dt;
                if (_nest.AoeTimer > S.Nest.AoeCooldown)
                {
                    _nest.AoeTimer = 0f;
                    float aoeR = S.Nest.AoeRadius;
                    for (int i = _enemies.Count - 1; i >= 0; i--)
                    {
                        if (Dist(_enemies[i].X, _enemies[i].Y, _nest.X, _nest.Y) < aoeR)
                            _enemies.RemoveAt(i);
                    }
                }
            }

            if (_player.IsEmitting)
            {
                for (int i = 0; i < _allies.Count; i++)
                {
                    var a = _allies[i];
                    if (IsInPlayerSignalCone(a.X, a.Y)) a.MoveTimer = S.Signal.NpcWakeDuration;
                    _allies[i] = a;
                }

                for (int i = 0; i < _enemies.Count; i++)
                {
                    var e = _enemies[i];
                    if (IsInPlayerSignalCone(e.X, e.Y)) e.MoveTimer = S.Signal.NpcWakeDuration;
                    _enemies[i] = e;
                }
            }

            if (_player.IsRepelling)
            {
                for (int i = 0; i < _allies.Count; i++)
                {
                    var a = _allies[i];
                    if (IsInPlayerRepelCone(a.X, a.Y)) a.MoveTimer = S.Signal.NpcWakeDuration;
                    _allies[i] = a;
                }

                for (int i = 0; i < _enemies.Count; i++)
                {
                    var e = _enemies[i];
                    if (IsInPlayerRepelCone(e.X, e.Y)) e.MoveTimer = S.Signal.NpcWakeDuration;
                    _enemies[i] = e;
                }
            }

            for (int ai = _allies.Count - 1; ai >= 0; ai--)
            {
                AllySim ally = _allies[ai];
                float ps = IsInPlayerSignalCone(ally.X, ally.Y) ? SignalStrength(_player.X, _player.Y, _player.SignalPower, ally.X, ally.Y) : 0f;
                float ns = SignalStrength(_nest.X, _nest.Y, _nest.SignalPower, ally.X, ally.Y);
                bool toPlayer = ps > ns;
                float tx = toPlayer ? _player.X : _nest.X;
                float ty = toPlayer ? _player.Y : _nest.Y;

                float ddx = tx - ally.X;
                float ddy = ty - ally.Y;
                float d = Mathf.Sqrt(ddx * ddx + ddy * ddy);
                if (d < 1e-6f) d = 1f;

                if (ally.MoveTimer > 0f)
                {
                    if (_player.IsRepelling && IsInPlayerRepelCone(ally.X, ally.Y))
                    {
                        float dp = Dist(ally.X, ally.Y, _player.X, _player.Y);
                        if (dp > 1e-3f)
                        {
                            float ux = (ally.X - _player.X) / dp;
                            float uy = (ally.Y - _player.Y) / dp;
                            float spd = ally.Speed * S.Player.RepelSpeedMult;
                            ally.X += ux * spd * dt;
                            ally.Y += uy * spd * dt;
                        }

                        ally.MoveTimer -= dt;
                    }
                    else
                    {
                        ally.X += ddx / d * ally.Speed * dt;
                        ally.Y += ddy / d * ally.Speed * dt;
                        ally.MoveTimer -= dt;
                    }
                }

                if (Dist(ally.X, ally.Y, _nest.X, _nest.Y) < _nest.Radius + ally.Radius)
                {
                    if (_nest.Absorbing)
                    {
                        _nest.Satiety += SatiationPerAbsorbed(SignalNpcKind.Green);
                        TryCompleteSatietyLevel();
                    }
                    else
                    {
                        _nest.Charge += 1f;
                        if (S.OptionNestHealFromGreen)
                            _nest.Hp = Mathf.Min(_nest.MaxHp, _nest.Hp + 1f);
                        if (_nest.Charge >= _nest.ChargeMax)
                        {
                            _nest.Charge = _nest.ChargeMax;
                            _nest.AbsorptionChargeStart = _nest.ChargeMax;
                            _nest.AbsorptionTimeLeft = S.Nest.AbsorptionDuration;
                            _nest.Absorbing = true;
                        }
                    }

                    _allies.RemoveAt(ai);
                    continue;
                }

                _allies[ai] = ally;
            }

            for (int ei = _enemies.Count - 1; ei >= 0; ei--)
            {
                EnemySim enemy = _enemies[ei];
                float psCone = IsInPlayerSignalCone(enemy.X, enemy.Y) ? SignalStrength(_player.X, _player.Y, _player.SignalPower, enemy.X, enemy.Y) : 0f;
                float ns = SignalStrength(_nest.X, _nest.Y, _nest.SignalPower, enemy.X, enemy.Y);
                bool signalPulls = IsInPlayerSignalCone(enemy.X, enemy.Y);

                float tx;
                float ty;
                if (signalPulls)
                {
                    tx = _player.X;
                    ty = _player.Y;
                }
                else
                {
                    tx = psCone > ns ? _player.X : _nest.X;
                    ty = psCone > ns ? _player.Y : _nest.Y;
                }

                float ddx = tx - enemy.X;
                float ddy = ty - enemy.Y;
                float d = Mathf.Sqrt(ddx * ddx + ddy * ddy);
                if (d < 1e-6f) d = 1f;

                float distP = Dist(enemy.X, enemy.Y, _player.X, _player.Y);
                bool inRepelCone = _player.IsRepelling && IsInPlayerRepelCone(enemy.X, enemy.Y);
                bool trapSlowIgnored = signalPulls || inRepelCone || (_nest.Absorbing && enemy.Kind == SignalNpcKind.Red);

                if (inRepelCone && distP > 1e-3f)
                {
                    float ux = (enemy.X - _player.X) / distP;
                    float uy = (enemy.Y - _player.Y) / distP;
                    var fx = GetPlayerTrapEffectsAt(enemy.X, enemy.Y);
                    bool baseSlow = !(_nest.Absorbing && enemy.Kind == SignalNpcKind.Red) && EnemyInBaseTrapZone(enemy.X, enemy.Y);
                    bool inSlow = baseSlow || (fx.InSlow && !trapSlowIgnored);
                    float rspd = enemy.Speed * S.Player.RepelSpeedMult * (inSlow ? S.Traps.SlowMult : 1f);
                    enemy.X += ux * rspd * dt;
                    enemy.Y += uy * rspd * dt;
                    enemy.MoveTimer -= dt;
                }
                else if (enemy.MoveTimer > 0f)
                {
                    var fx = GetPlayerTrapEffectsAt(enemy.X, enemy.Y);
                    bool attractHere = fx.Attract.HasValue && !signalPulls;
                    if (attractHere)
                    {
                        Vector2 pt = fx.Attract.Value;
                        float adx = pt.x - enemy.X;
                        float ady = pt.y - enemy.Y;
                        float ad = Mathf.Sqrt(adx * adx + ady * ady);
                        if (ad < 1e-6f) ad = 1f;
                        float pullSpd = enemy.Speed * S.Traps.AttractPullSpeedMult;
                        enemy.X += adx / ad * pullSpd * dt;
                        enemy.Y += ady / ad * pullSpd * dt;
                        enemy.MoveTimer -= dt;
                    }
                    else
                    {
                        bool baseSlow = !(_nest.Absorbing && enemy.Kind == SignalNpcKind.Red) && EnemyInBaseTrapZone(enemy.X, enemy.Y);
                        bool inSlow = baseSlow || (fx.InSlow && !trapSlowIgnored);
                        bool trapSlow = inSlow && !trapSlowIgnored;
                        float spdMul = trapSlow ? S.Traps.SlowMult : 1f;
                        enemy.X += ddx / d * enemy.Speed * spdMul * dt;
                        enemy.Y += ddy / d * enemy.Speed * spdMul * dt;
                        enemy.MoveTimer -= dt;
                    }
                }

                if (Dist(enemy.X, enemy.Y, _nest.X, _nest.Y) < _nest.Radius + enemy.Radius)
                {
                    if (_nest.Absorbing)
                    {
                        _nest.Satiety += SatiationPerAbsorbed(enemy.Kind);
                        TryCompleteSatietyLevel();
                        _enemies.RemoveAt(ei);
                        continue;
                    }

                    float dmg = NestDamageForKind(enemy.Kind);
                    if (!(enemy.Kind == SignalNpcKind.Red && S.CheatNoRedEnemyNestDamage))
                    {
                        _nest.Hp = Mathf.Max(0f, _nest.Hp - dmg);
                        if (_nest.Hp <= 0f)
                        {
                            EndDefeat();
                            return;
                        }
                    }

                    _enemies.RemoveAt(ei);
                    continue;
                }

                _enemies[ei] = enemy;
            }

            if (_nest.Absorbing)
            {
                _nest.AbsorptionTimeLeft -= dt;
                if (_nest.AbsorptionTimeLeft <= 0f)
                {
                    _nest.AbsorptionTimeLeft = 0f;
                    _nest.Charge = 0f;
                    _nest.Absorbing = false;
                }
                else
                {
                    float u = _nest.AbsorptionTimeLeft / Mathf.Max(1e-6f, S.Nest.AbsorptionDuration);
                    _nest.Charge = _nest.AbsorptionChargeStart * u;
                }
            }
        }

        private void EndDefeat()
        {
            _phase = SignalRunPhase.Dead;
            if (GameManager.HasInstance)
            {
                GameManager.Instance.AddScore(Mathf.RoundToInt(_nest.Level));
                GameManager.Instance.GameOver();
            }
        }

        private void EndVictory()
        {
            _phase = SignalRunPhase.Win;
            if (GameManager.HasInstance)
            {
                GameManager.Instance.AddScore(Mathf.RoundToInt(_nest.Level));
                GameManager.Instance.Victory();
            }
        }

        private void TryCompleteSatietyLevel()
        {
            var S = _balance.Nest;
            if (_nest.Satiety < S.SatiationMax) return;
            _nest.Satiety = 0f;
            if (_nest.Level >= S.MaxLevel) return;

            _nest.Level++;
            _nest.ChargeMax = _balance.Nest.ChargeBase + _nest.Level * _balance.Nest.ChargePerLevel;
            _nest.SignalPower += _balance.Nest.LevelSignalPowerBonus;
            _nest.Radius += _balance.Nest.RadiusPerLevel;
            if (_nest.Level >= _balance.Nest.AllyDeliveryPlayerMaxHpFromLevel)
                _player.MaxHp += _balance.Nest.AllyDeliveryPlayerMaxHpBonus;

            if (_nest.Level >= _balance.Win.MinNestLevel)
            {
                EndVictory();
                return;
            }

            _phase = SignalRunPhase.EvolutionPick;
            _evolutionModalChannel?.Raise(true);
        }

        private float NestPulseMaxR()
        {
            return Mathf.Max(_balance.Nest.PulseMaxRadius, Mathf.Sqrt(_w * _w + _h * _h) * 1.15f);
        }

        private float NestDamageForKind(SignalNpcKind kind) =>
            kind == SignalNpcKind.Red ? _balance.Enemy.NestDamageRed : _balance.Enemy.NestDamageGreen;

        private float SatiationPerAbsorbed(SignalNpcKind kind) =>
            kind == SignalNpcKind.Red ? _balance.Enemy.SatiationRed : _balance.Enemy.SatiationGreen;

        private float SignalStrength(float sx, float sy, float power, float tx, float ty)
        {
            float d = Mathf.Max(1f, Dist(sx, sy, tx, ty));
            return power / (d * d) * _balance.Signal.StrengthScale;
        }

        private bool IsInPlayerSignalCone(float ex, float ey)
        {
            if (!_player.IsEmitting) return false;
            if (Dist(ex, ey, _player.X, _player.Y) >= _player.SignalRadius) return false;
            float angleToEnt = LogicalAngleFromTo(_player.X, _player.Y, ex, ey);
            float diff = angleToEnt - _player.Angle;
            while (diff > Mathf.PI) diff -= Mathf.PI * 2f;
            while (diff < -Mathf.PI) diff += Mathf.PI * 2f;
            return Mathf.Abs(diff) < _balance.Signal.ConeAngleRadians * 0.5f;
        }

        private bool IsInPlayerRepelCone(float ex, float ey)
        {
            if (!_player.IsRepelling) return false;
            if (Dist(ex, ey, _player.X, _player.Y) >= _player.SignalRadius) return false;
            float angleToEnt = LogicalAngleFromTo(_player.X, _player.Y, ex, ey);
            float diff = angleToEnt - _player.Angle;
            while (diff > Mathf.PI) diff -= Mathf.PI * 2f;
            while (diff < -Mathf.PI) diff += Mathf.PI * 2f;
            return Mathf.Abs(diff) < _balance.Signal.ConeAngleRadians * 0.5f;
        }

        private bool EnemyInBaseTrapZone(float ex, float ey)
        {
            for (int i = 0; i < _trapZones.Count; i++)
            {
                var z = _trapZones[i];
                if (Dist(ex, ey, z.X, z.Y) < z.R) return true;
            }

            return false;
        }

        private (bool InSlow, Vector2? Attract) GetPlayerTrapEffectsAt(float ex, float ey)
        {
            bool inSlow = false;
            Vector2? attract = null;
            float best = 0f;
            for (int i = 0; i < _playerTraps.Count; i++)
            {
                var t = _playerTraps[i];
                float dd = Dist(ex, ey, t.X, t.Y);
                if (dd >= t.R) continue;
                if (t.Kind == PlayerTrapKind.Slow) inSlow = true;
                else if (!attract.HasValue || dd < best)
                {
                    attract = new Vector2(t.X, t.Y);
                    best = dd;
                }
            }

            return (inSlow, attract);
        }

        private void GenerateTrapZones()
        {
            _trapZones.Clear();
            var T = _balance.Traps;
            int n = T.Count;
            float sector = (Mathf.PI * 2f) / Mathf.Max(1, n);
            float edge = T.SectorEdgeMargin * sector;
            float rot = UnityEngine.Random.value * Mathf.PI * 2f;
            float gap = T.MinGapBetween;

            for (int i = 0; i < n; i++)
            {
                float a0 = rot + i * sector + edge;
                float a1 = rot + (i + 1) * sector - edge;
                bool placed = false;
                for (int attempt = 0; attempt < 48 && !placed; attempt++)
                {
                    float angle = a0 + UnityEngine.Random.value * (a1 - a0);
                    float ring = T.RingInner + UnityEngine.Random.value * (T.RingOuter - T.RingInner);
                    float rad = T.RadiusMin + UnityEngine.Random.value * (T.RadiusMax - T.RadiusMin);
                    float x = _nest.X + Mathf.Cos(angle) * ring;
                    float y = _nest.Y + Mathf.Sin(angle) * ring;
                    x = Mathf.Clamp(x, rad + 4f, _w - rad - 4f);
                    y = Mathf.Clamp(y, rad + 4f, _h - rad - 4f);
                    var cand = new TrapZoneSim { X = x, Y = y, R = rad };
                    bool ok = true;
                    for (int j = 0; j < _trapZones.Count; j++)
                    {
                        if (TrapZonesTooClose(cand, _trapZones[j], gap))
                        {
                            ok = false;
                            break;
                        }
                    }

                    if (ok)
                    {
                        _trapZones.Add(cand);
                        placed = true;
                    }
                }

                if (!placed)
                {
                    float angle = (a0 + a1) * 0.5f;
                    float ring = (T.RingInner + T.RingOuter) * 0.5f;
                    float baseR = (T.RadiusMin + T.RadiusMax) * 0.5f;
                    for (float scale = 1f; scale >= 0.4f; scale -= 0.08f)
                    {
                        float rad = Mathf.Max(T.RadiusMin * 0.65f, baseR * scale);
                        float x = _nest.X + Mathf.Cos(angle) * ring;
                        float y = _nest.Y + Mathf.Sin(angle) * ring;
                        x = Mathf.Clamp(x, rad + 4f, _w - rad - 4f);
                        y = Mathf.Clamp(y, rad + 4f, _h - rad - 4f);
                        var cand = new TrapZoneSim { X = x, Y = y, R = rad };
                        bool ok = true;
                        for (int j = 0; j < _trapZones.Count; j++)
                        {
                            if (TrapZonesTooClose(cand, _trapZones[j], gap))
                            {
                                ok = false;
                                break;
                            }
                        }

                        if (ok)
                        {
                            _trapZones.Add(cand);
                            break;
                        }
                    }
                }
            }
        }

        private void AddBonusTrapZone()
        {
            var T = _balance.Traps;
            float gap = T.MinGapBetween;
            const float margin = 8f;
            for (int attempt = 0; attempt < 72; attempt++)
            {
                float rad = T.RadiusMin + UnityEngine.Random.value * (T.RadiusMax - T.RadiusMin);
                float x = rad + margin + UnityEngine.Random.value * (_w - 2f * (rad + margin));
                float y = rad + margin + UnityEngine.Random.value * (_h - 2f * (rad + margin));
                var cand = new TrapZoneSim { X = x, Y = y, R = rad };
                if (Dist(cand.X, cand.Y, _nest.X, _nest.Y) < _nest.Radius + rad + 28f) continue;
                bool ok = true;
                for (int j = 0; j < _trapZones.Count; j++)
                {
                    if (TrapZonesTooClose(cand, _trapZones[j], gap))
                    {
                        ok = false;
                        break;
                    }
                }

                if (!ok) continue;
                for (int p = 0; p < _playerTraps.Count; p++)
                {
                    var pt = _playerTraps[p];
                    if (Dist(cand.X, cand.Y, pt.X, pt.Y) < rad + pt.R + gap)
                    {
                        ok = false;
                        break;
                    }
                }

                if (ok)
                {
                    _trapZones.Add(cand);
                    return;
                }
            }
        }

        private static bool TrapZonesTooClose(TrapZoneSim a, TrapZoneSim b, float gap)
        {
            return Dist(a.X, a.Y, b.X, b.Y) < a.R + b.R + gap;
        }

        private void TryPlacePlayerTrap(PlayerTrapKind kind)
        {
            if (_phase != SignalRunPhase.Playing) return;
            if (kind == PlayerTrapKind.Slow) PlaceOneTrap(ref _trapSlow, kind);
            else PlaceOneTrap(ref _trapAttract, kind);
        }

        private void PlaceOneTrap(ref TrapStock tr, PlayerTrapKind kind)
        {
            int maxC = _balance.Traps.PlayerTrapMaxCharges;
            float recharge = kind == PlayerTrapKind.Slow ? _balance.Traps.PlayerTrapRechargeSlow : _balance.Traps.PlayerTrapRechargeAttract;
            if (tr.Charges <= 0) return;
            tr.Charges--;
            if (tr.Charges < maxC) tr.CooldownLeft = recharge;
            _playerTraps.Add(new PlayerTrapSim
            {
                X = _player.X,
                Y = _player.Y,
                R = _balance.Traps.PlayerPlacedRadius,
                TimeLeft = _balance.Traps.PlayerPlacedDuration,
                Kind = kind,
            });
        }

        private static void StepTrapStock(ref TrapStock tr, int maxC, float recharge, float dt)
        {
            if (tr.Charges >= maxC)
            {
                tr.CooldownLeft = 0f;
                return;
            }

            tr.CooldownLeft -= dt;
            if (tr.CooldownLeft <= 0f)
            {
                tr.Charges++;
                tr.CooldownLeft = tr.Charges >= maxC ? 0f : recharge;
            }
        }

        private static float TrapBarPercent(TrapStock tr, int maxC, float recharge)
        {
            if (maxC <= 0) return 0f;
            float b = tr.Charges / (float)maxC * 100f;
            if (tr.Charges >= maxC) return 100f;
            float partial = (1f - Mathf.Max(0f, tr.CooldownLeft) / Mathf.Max(1e-6f, recharge)) * (100f / maxC);
            return Mathf.Min(100f, b + partial);
        }

        private SignalWaveEntry? GetSpawnWaveDef()
        {
            var waves = _balance.Spawn.Waves;
            if (waves == null || waves.Count == 0) return null;
            int idx = (_waveNumber - 1) % waves.Count;
            return waves[idx];
        }

        private void RebuildEnemySpawnSchedule()
        {
            _waveSpawnTimes.Clear();
            _waveSpawnNext.Clear();
            _waveDisplayIndex = _waveNumber;
            var wdef = GetSpawnWaveDef();
            if (!wdef.HasValue || !(wdef.Value.Duration > 0f)) return;

            AddKindSchedule("red", wdef.Value.Red, wdef.Value.Duration);
            AddKindSchedule("green", wdef.Value.Green, wdef.Value.Duration);
        }

        private void AddKindSchedule(string kind, int count, float duration)
        {
            int n = Mathf.Max(0, count);
            if (n <= 0) return;
            var times = new List<float>(n);
            for (int i = 0; i < n; i++) times.Add((i + 1f) / (n + 1f) * duration);
            _waveSpawnTimes[kind] = times;
            _waveSpawnNext[kind] = 0;
        }

        private void FlushEnemySpawnsUpTo(float tEnd)
        {
            foreach (var kv in _waveSpawnTimes)
            {
                string kind = kv.Key;
                var times = kv.Value;
                int idx = _waveSpawnNext.TryGetValue(kind, out int v) ? v : 0;
                while (idx < times.Count && tEnd + 1e-9f >= times[idx])
                {
                    if (kind == "green") SpawnAlly();
                    else SpawnEnemy(SignalNpcKind.Red);
                    idx++;
                }

                _waveSpawnNext[kind] = idx;
            }
        }

        private void UpdateWaveEnemySpawns(float dt)
        {
            var wdef = GetSpawnWaveDef();
            if (!wdef.HasValue || !(wdef.Value.Duration > 0f)) return;

            _waveElapsed += dt;
            while (wdef.HasValue && wdef.Value.Duration > 0f && _waveElapsed >= wdef.Value.Duration)
            {
                FlushEnemySpawnsUpTo(wdef.Value.Duration);
                _waveElapsed -= wdef.Value.Duration;
                _waveNumber++;
                RebuildEnemySpawnSchedule();
                wdef = GetSpawnWaveDef();
            }

            FlushEnemySpawnsUpTo(_waveElapsed);
        }

        private void SpawnEnemy(SignalNpcKind kind)
        {
            float r = GetSpawnRingRadiusLogical();
            float ang = UnityEngine.Random.value * Mathf.PI * 2f;
            float x = _player.X + Mathf.Cos(ang) * r;
            float y = _player.Y + Mathf.Sin(ang) * r;

            float speed = _balance.Enemy.SpeedMin + UnityEngine.Random.value * _balance.Enemy.SpeedRand;
            _enemies.Add(new EnemySim
            {
                X = x,
                Y = y,
                Speed = speed,
                NestDamage = NestDamageForKind(kind),
                Radius = _balance.Enemy.Radius,
                Kind = kind,
                MoveTimer = 0f,
            });
        }

        private void SpawnAlly()
        {
            float r = GetSpawnRingRadiusLogical();
            float ang = UnityEngine.Random.value * Mathf.PI * 2f;
            float x = _player.X + Mathf.Cos(ang) * r;
            float y = _player.Y + Mathf.Sin(ang) * r;

            _allies.Add(new AllySim
            {
                X = x,
                Y = y,
                Speed = _balance.Ally.SpeedMin + UnityEngine.Random.value * _balance.Ally.SpeedRand,
                Radius = _balance.Ally.Radius,
                MoveTimer = 0f,
            });
        }

        private static float Dist(float ax, float ay, float bx, float by)
        {
            float dx = ax - bx;
            float dy = ay - by;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Угол в радианах в плоскости мира (как у Unity: 0 = +X, π/2 = +Y), согласованный с <see cref="LogicalToWorld"/>.
        /// В логике ly растёт вниз (canvas), поэтому вертикальная составляющая инвертируется относительно «школьного» Atan2(dy,dx).
        /// </summary>
        private static float LogicalAngleFromTo(float fromX, float fromY, float toX, float toY)
        {
            return Mathf.Atan2(-(toY - fromY), toX - fromX);
        }

        private void PushHud()
        {
            if (_hudChannel == null || _balance == null) return;
            float satMax = Mathf.Max(1e-6f, _balance.Nest.SatiationMax);
            float dashPct = 100f;
            float dcd = _balance.Player.DashCooldown;
            if (_player.DashTimeLeft > 0f) dashPct = 100f;
            else if (_player.DashCooldownLeft > 0f && dcd > 0f) dashPct = (1f - _player.DashCooldownLeft / dcd) * 100f;

            var snap = new SignalHudSnapshot(
                Mathf.Clamp01(_nest.Charge / Mathf.Max(1e-6f, _nest.ChargeMax)),
                Mathf.Clamp01(_nest.Hp / Mathf.Max(1e-6f, _nest.MaxHp)),
                Mathf.Clamp01(_nest.Satiety / satMax),
                _nest.Level,
                _waveDisplayIndex,
                _enemies.Count,
                Mathf.Clamp01(dashPct / 100f),
                TrapBarPercent(_trapSlow, _balance.Traps.PlayerTrapMaxCharges, _balance.Traps.PlayerTrapRechargeSlow) / 100f,
                TrapBarPercent(_trapAttract, _balance.Traps.PlayerTrapMaxCharges, _balance.Traps.PlayerTrapRechargeAttract) / 100f
            );
            _hudChannel.Raise(snap);
        }

        private void SyncTransforms()
        {
            if (_playerTransform == null) return;
            Vector3 pw = LogicalToWorld(_player.X, _player.Y);
            _playerTransform.position = new Vector3(pw.x, pw.y, _playerTransform.position.z);
            _playerTransform.rotation = Quaternion.identity;
        }

        private Vector3 LogicalToWorld(float lx, float ly)
        {
            float wx = (lx - _w * 0.5f) / _pixelsPerUnit;
            float wy = -(ly - _h * 0.5f) / _pixelsPerUnit;
            return new Vector3(wx, wy, 0f);
        }
    }

    internal struct PlayerSim
    {
        public float X, Y;
        public float Speed;
        public float Hp, MaxHp;
        public float SignalPower, SignalRadius;
        public float Radius;
        public float SignalJam;
        public float MoveSlowTimer;
        public float DashTimeLeft, DashCooldownLeft;
        public float DashDirX, DashDirY;
        public float Angle;
        public bool IsEmitting;
        public bool IsRepelling;
    }

    internal struct NestSim
    {
        public float X, Y;
        public int Level;
        public float Hp, MaxHp;
        public float Charge, ChargeMax;
        public float Satiety;
        public bool Absorbing;
        public float AbsorptionTimeLeft;
        public float AbsorptionChargeStart;
        public float HealRadius;
        public float SignalPower;
        public float Radius;
        public float AoeTimer;
        public float PulseTimer;
    }

    internal struct AllySim
    {
        public float X, Y;
        public float Speed;
        public float Radius;
        public float MoveTimer;
    }

    internal struct EnemySim
    {
        public float X, Y;
        public float Speed;
        public float NestDamage;
        public float Radius;
        public SignalNpcKind Kind;
        public float MoveTimer;
    }

    internal struct PulseSim
    {
        public float R;
        public float Speed;
    }

    internal struct TrapZoneSim
    {
        public float X, Y, R;
    }

    internal enum PlayerTrapKind
    {
        Slow,
        Attract,
    }

    internal struct PlayerTrapSim
    {
        public float X, Y, R;
        public float TimeLeft;
        public PlayerTrapKind Kind;
    }

    internal struct TrapStock
    {
        public int Charges;
        public float CooldownLeft;
    }
}
