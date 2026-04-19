using UnityEngine;
using UnityEngine.Events;

namespace LudumDare.Template.Events
{
    [CreateAssetMenu(menuName = "LudumDare/Events/Void Event Channel", fileName = "VoidEventChannel")]
    public class VoidEventChannelSO : ScriptableObject
    {
        public UnityAction OnEventRaised;

        public void Raise() => OnEventRaised?.Invoke();
    }
}
