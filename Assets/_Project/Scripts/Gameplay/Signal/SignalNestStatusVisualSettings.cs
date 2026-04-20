using UnityEngine;

namespace LudumDare.Template.Gameplay.Signal
{
    [CreateAssetMenu(menuName = "LudumDare/Signal/Nest Status Visual Settings", fileName = "SignalNestStatusVisualSettings")]
    public sealed class SignalNestStatusVisualSettings : ScriptableObject
    {
        [Header("Colors")]
        [SerializeField] private Color _foodFillColor = new(1f, 0.85f, 0.2f, 1f);
        [SerializeField] private Color _healthFillColor = new(0.35f, 0.95f, 0.45f, 1f);
        [SerializeField] private Color _chargeFillColor = new(0.35f, 0.8f, 1f, 1f);
        [SerializeField] private Color _feedingCountdownColor = new(1f, 0.45f, 0.25f, 1f);
        [SerializeField] private Color _barBackgroundColor = new(0f, 0f, 0f, 0.5f);

        [Header("Linear Bars")]
        [SerializeField, Min(0.01f)] private float _barWidth = 0.95f;
        [SerializeField, Min(0.01f)] private float _barHeight = 0.12f;
        [SerializeField] private float _topBarOffsetY = 1.05f;
        [SerializeField] private float _bottomBarOffsetY = -1.05f;

        [Header("Radial Ring")]
        [SerializeField, Min(0.01f)] private float _ringRadius = 1.15f;
        [SerializeField, Min(0.001f)] private float _ringThickness = 0.07f;
        [SerializeField, Range(16, 256)] private int _ringSegments = 96;
        [SerializeField] private float _ringStartAngleDeg = 90f;

        [Header("Scaling")]
        [Tooltip("Множитель масштаба корня UI (полоски, кольцо) относительно значения, которое приходит из SignalGameplayView (там же настраивается, насколько UI следует за птенцом).")]
        [SerializeField, Min(0.01f)] private float _scaleByChickMultiplier = 1f;

        [Header("Sorting")]
        [SerializeField] private int _barSortingOrder = 12;
        [SerializeField] private int _ringSortingOrder = 13;

        public Color FoodFillColor => _foodFillColor;
        public Color HealthFillColor => _healthFillColor;
        public Color ChargeFillColor => _chargeFillColor;
        public Color FeedingCountdownColor => _feedingCountdownColor;
        public Color BarBackgroundColor => _barBackgroundColor;
        public float BarWidth => _barWidth;
        public float BarHeight => _barHeight;
        public float TopBarOffsetY => _topBarOffsetY;
        public float BottomBarOffsetY => _bottomBarOffsetY;
        public float RingRadius => _ringRadius;
        public float RingThickness => _ringThickness;
        public int RingSegments => _ringSegments;
        public float RingStartAngleDeg => _ringStartAngleDeg;
        public float ScaleByChickMultiplier => _scaleByChickMultiplier;
        public int BarSortingOrder => _barSortingOrder;
        public int RingSortingOrder => _ringSortingOrder;
    }
}
