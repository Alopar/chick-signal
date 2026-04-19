using UnityEngine;

namespace LudumDare.Template.Gameplay.Signal
{
    /// <summary>
    /// Модалка выбора бонуса эволюции (OnGUI, без префаба).
    /// </summary>
    [DefaultExecutionOrder(-35)]
    public sealed class SignalEvolutionPanel : MonoBehaviour
    {
        [SerializeField] private SignalGameController _controller;
        private bool _visible;

        private void Awake()
        {
            if (_controller == null) _controller = FindAnyObjectByType<SignalGameController>();
        }

        private void OnEnable()
        {
            if (_controller == null) _controller = FindAnyObjectByType<SignalGameController>();
            if (_controller != null && _controller.EvolutionChannel != null)
                _controller.EvolutionChannel.OnVisibilityChanged += HandleVisibility;
        }

        private void OnDisable()
        {
            if (_controller != null && _controller.EvolutionChannel != null)
                _controller.EvolutionChannel.OnVisibilityChanged -= HandleVisibility;
        }

        private void HandleVisibility(bool visible) => _visible = visible;

        private void OnGUI()
        {
            if (!_visible || _controller == null) return;

            float w = 420f;
            float h = 220f;
            var r = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            GUI.Box(r, "Nest evolution — pick one");
            float y = r.y + 28f;
            float lh = 36f;
            if (GUI.Button(new Rect(r.x + 16f, y, w - 32f, lh), "Full heal +10 max HP"))
            {
                _controller.ApplyEvolutionBonus(SignalEvolutionBonus.Heal);
            }

            y += lh + 8f;
            if (GUI.Button(new Rect(r.x + 16f, y, w - 32f, lh), "Extra slow trap zone"))
            {
                _controller.ApplyEvolutionBonus(SignalEvolutionBonus.Trap);
            }

            y += lh + 8f;
            if (GUI.Button(new Rect(r.x + 16f, y, w - 32f, lh), "Purge all enemies"))
            {
                _controller.ApplyEvolutionBonus(SignalEvolutionBonus.Purge);
            }
        }
    }
}
