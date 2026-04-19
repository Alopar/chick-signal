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

        private void Awake()
        {
            if (_animator == null) _animator = GetComponent<Animator>();
            if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void SetAbsorbing(bool absorbing)
        {
            if (_animator != null)
                _animator.SetBool(AbsorbingParameter, absorbing);
        }

        public SpriteRenderer SpriteRenderer => _spriteRenderer;
    }
}
