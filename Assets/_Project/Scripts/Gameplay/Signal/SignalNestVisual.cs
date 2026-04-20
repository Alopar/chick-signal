using UnityEngine;

namespace LudumDare.Template.Gameplay.Signal
{
    /// <summary>
    /// Управляет параметром аниматора гнезда (спокойное / поглощение).
    /// </summary>
    public sealed class SignalNestVisual : MonoBehaviour
    {
        public const string AbsorbingParameter = "Absorbing";

        [SerializeField] private Animator _animator;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Transform _chickVisualRoot;
        [SerializeField] private Transform _pillowVisualRoot;

        private void Awake()
        {
            if (_animator == null) _animator = GetComponent<Animator>();
            if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_chickVisualRoot == null) _chickVisualRoot = transform;
        }

        public void SetAbsorbing(bool absorbing)
        {
            if (_animator != null)
                _animator.SetBool(AbsorbingParameter, absorbing);
        }

        public void SetChickScale(float scale)
        {
            if (_chickVisualRoot == null)
                _chickVisualRoot = transform;

            float safeScale = Mathf.Max(0.01f, scale);
            _chickVisualRoot.localScale = new Vector3(safeScale, safeScale, 1f);

            if (_pillowVisualRoot != null)
            {
                float inverse = 1f / safeScale;
                _pillowVisualRoot.localScale = new Vector3(inverse, inverse, 1f);
            }
        }

        public SpriteRenderer SpriteRenderer => _spriteRenderer;
    }
}
