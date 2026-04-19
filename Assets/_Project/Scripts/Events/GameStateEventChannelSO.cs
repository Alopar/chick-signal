using LudumDare.Template.Managers;
using UnityEngine;
using UnityEngine.Events;

namespace LudumDare.Template.Events
{
    [CreateAssetMenu(menuName = "LudumDare/Events/Game State Event Channel", fileName = "GameStateEventChannel")]
    public class GameStateEventChannelSO : ScriptableObject
    {
        public UnityAction<GameState> OnEventRaised;

        public void Raise(GameState state) => OnEventRaised?.Invoke(state);
    }
}
