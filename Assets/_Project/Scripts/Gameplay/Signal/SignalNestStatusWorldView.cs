using UnityEngine;

namespace LudumDare.Template.Gameplay.Signal
{
    /// <summary>
    /// World-space индикаторы состояния гнезда, закрепленные на префабе гнезда.
    /// </summary>
    public sealed class SignalNestStatusWorldView : MonoBehaviour
    {
        [SerializeField] private SignalNestStatusVisualSettings _settings;

        [Header("Optional prefab references")]
        [SerializeField] private Transform _topBarRoot;
        [SerializeField] private LineRenderer _topBarBackground;
        [SerializeField] private LineRenderer _topBarFill;
        [SerializeField] private Transform _bottomBarRoot;
        [SerializeField] private LineRenderer _bottomBarBackground;
        [SerializeField] private LineRenderer _bottomBarFill;
        [SerializeField] private LineRenderer _ring;

        public void SetSettings(SignalNestStatusVisualSettings settings)
        {
            _settings = settings;
            RefreshStaticVisual();
        }

        public void ApplyStatus(
            float satiety01,
            float hp01,
            float charge01,
            bool absorbing,
            float absorptionLeft01,
            float chickScale)
        {
            EnsureVisuals();

            float safeSatiety = Mathf.Clamp01(satiety01);
            float safeHp = Mathf.Clamp01(hp01);
            float safeCharge = Mathf.Clamp01(charge01);
            float safeAbsorbLeft = Mathf.Clamp01(absorptionLeft01);
            float safeScale = Mathf.Max(0.01f, chickScale * GetScaleMultiplier());

            transform.localScale = new Vector3(safeScale, safeScale, 1f);

            float barWidth = GetBarWidth();
            float barHeight = GetBarHeight();
            if (_topBarRoot != null) _topBarRoot.localPosition = new Vector3(0f, GetTopOffsetY(), 0f);
            if (_bottomBarRoot != null) _bottomBarRoot.localPosition = new Vector3(0f, GetBottomOffsetY(), 0f);

            ApplyLinearBar(_topBarBackground, _topBarFill, safeSatiety, barWidth, barHeight, GetFoodColor());
            ApplyLinearBar(_bottomBarBackground, _bottomBarFill, safeHp, barWidth, barHeight, GetHealthColor());

            float ringProgress = absorbing ? safeAbsorbLeft : safeCharge;
            Color ringColor = absorbing ? GetFeedingCountdownColor() : GetChargeColor();
            ApplyRing(ringProgress, ringColor);
        }

        private void Awake()
        {
            EnsureVisuals();
            RefreshStaticVisual();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying) return;
            EnsureVisuals();
            RefreshStaticVisual();
        }

        private void EnsureVisuals()
        {
            if (_topBarRoot == null)
                _topBarRoot = EnsureChild("FoodBar");
            if (_bottomBarRoot == null)
                _bottomBarRoot = EnsureChild("HealthBar");

            if (_topBarBackground == null)
                _topBarBackground = EnsureLine(_topBarRoot, "Background");
            if (_topBarFill == null)
                _topBarFill = EnsureLine(_topBarRoot, "Fill");

            if (_bottomBarBackground == null)
                _bottomBarBackground = EnsureLine(_bottomBarRoot, "Background");
            if (_bottomBarFill == null)
                _bottomBarFill = EnsureLine(_bottomBarRoot, "Fill");

            if (_ring == null)
            {
                var ringRoot = EnsureChild("ChargeRing");
                _ring = ringRoot.GetComponent<LineRenderer>();
                if (_ring == null) _ring = ringRoot.gameObject.AddComponent<LineRenderer>();
            }

            ConfigureLineRenderer(_topBarBackground);
            ConfigureLineRenderer(_topBarFill);
            ConfigureLineRenderer(_bottomBarBackground);
            ConfigureLineRenderer(_bottomBarFill);
            ConfigureLineRenderer(_ring);
        }

        private void RefreshStaticVisual()
        {
            if (_topBarBackground == null || _topBarFill == null || _bottomBarBackground == null || _bottomBarFill == null || _ring == null)
                return;

            SetLineColor(_topBarBackground, GetBarBackgroundColor());
            SetLineColor(_bottomBarBackground, GetBarBackgroundColor());
            SetLineColor(_topBarFill, GetFoodColor());
            SetLineColor(_bottomBarFill, GetHealthColor());

            int barOrder = GetBarSortingOrder();
            _topBarBackground.sortingOrder = barOrder;
            _topBarFill.sortingOrder = barOrder + 1;
            _bottomBarBackground.sortingOrder = barOrder;
            _bottomBarFill.sortingOrder = barOrder + 1;
            _ring.sortingOrder = GetRingSortingOrder();
        }

        private static void ApplyLinearBar(LineRenderer bg, LineRenderer fill, float value01, float width, float height, Color fillColor)
        {
            if (bg == null || fill == null) return;
            float halfWidth = width * 0.5f;
            float leftX = -halfWidth;
            float rightX = halfWidth;
            float fillRight = Mathf.Lerp(leftX, rightX, Mathf.Clamp01(value01));

            bg.positionCount = 2;
            bg.useWorldSpace = false;
            bg.loop = false;
            bg.widthMultiplier = height;
            bg.SetPosition(0, new Vector3(leftX, 0f, 0f));
            bg.SetPosition(1, new Vector3(rightX, 0f, 0f));

            fill.positionCount = 2;
            fill.useWorldSpace = false;
            fill.loop = false;
            fill.widthMultiplier = height;
            fill.SetPosition(0, new Vector3(leftX, 0f, 0f));
            fill.SetPosition(1, new Vector3(fillRight, 0f, 0f));
            SetLineColor(fill, fillColor);
        }

        private void ApplyRing(float progress01, Color color)
        {
            if (_ring == null) return;

            float radius = GetRingRadius();
            float width = GetRingThickness();
            int segments = Mathf.Max(16, GetRingSegments());
            float clamped = Mathf.Clamp01(progress01);

            SetLineColor(_ring, color);
            _ring.widthMultiplier = width;

            if (clamped <= 0.0001f)
            {
                _ring.positionCount = 0;
                return;
            }

            int pointCount = Mathf.Max(2, Mathf.RoundToInt(segments * clamped) + 1);
            _ring.positionCount = pointCount;
            _ring.loop = false;

            float startRad = GetRingStartAngleDeg() * Mathf.Deg2Rad;
            float spanRad = Mathf.PI * 2f * clamped;
            float denom = Mathf.Max(1, pointCount - 1);

            for (int i = 0; i < pointCount; i++)
            {
                float t = i / denom;
                float angle = startRad - spanRad * t;
                _ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
            }
        }

        private Transform EnsureChild(string childName)
        {
            var child = transform.Find(childName);
            if (child != null) return child;
            var go = new GameObject(childName);
            go.transform.SetParent(transform, false);
            return go.transform;
        }

        private static LineRenderer EnsureLine(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            GameObject go;
            if (child == null)
            {
                go = new GameObject(childName);
                go.transform.SetParent(parent, false);
            }
            else
            {
                go = child.gameObject;
            }

            var lr = go.GetComponent<LineRenderer>();
            if (lr == null) lr = go.AddComponent<LineRenderer>();
            return lr;
        }

        private static void ConfigureLineRenderer(LineRenderer lr)
        {
            if (lr == null) return;
            lr.useWorldSpace = false;
            lr.textureMode = LineTextureMode.Stretch;
            lr.numCapVertices = 2;
            lr.alignment = LineAlignment.TransformZ;
            if (lr.sharedMaterial == null)
                lr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        }

        private static void SetLineColor(LineRenderer lr, Color color)
        {
            if (lr == null) return;
            lr.startColor = color;
            lr.endColor = color;
        }

        private Color GetFoodColor() => _settings != null ? _settings.FoodFillColor : new Color(1f, 0.85f, 0.2f, 1f);
        private Color GetHealthColor() => _settings != null ? _settings.HealthFillColor : new Color(0.35f, 0.95f, 0.45f, 1f);
        private Color GetChargeColor() => _settings != null ? _settings.ChargeFillColor : new Color(0.35f, 0.8f, 1f, 1f);
        private Color GetFeedingCountdownColor() => _settings != null ? _settings.FeedingCountdownColor : new Color(1f, 0.45f, 0.25f, 1f);
        private Color GetBarBackgroundColor() => _settings != null ? _settings.BarBackgroundColor : new Color(0f, 0f, 0f, 0.5f);
        private float GetBarWidth() => _settings != null ? _settings.BarWidth : 0.95f;
        private float GetBarHeight() => _settings != null ? _settings.BarHeight : 0.12f;
        private float GetTopOffsetY() => _settings != null ? _settings.TopBarOffsetY : 1.05f;
        private float GetBottomOffsetY() => _settings != null ? _settings.BottomBarOffsetY : -1.05f;
        private float GetRingRadius() => _settings != null ? _settings.RingRadius : 1.15f;
        private float GetRingThickness() => _settings != null ? _settings.RingThickness : 0.07f;
        private int GetRingSegments() => _settings != null ? _settings.RingSegments : 96;
        private float GetRingStartAngleDeg() => _settings != null ? _settings.RingStartAngleDeg : 90f;
        private float GetScaleMultiplier() => _settings != null ? _settings.ScaleByChickMultiplier : 1f;
        private int GetBarSortingOrder() => _settings != null ? _settings.BarSortingOrder : 12;
        private int GetRingSortingOrder() => _settings != null ? _settings.RingSortingOrder : 13;
    }
}
