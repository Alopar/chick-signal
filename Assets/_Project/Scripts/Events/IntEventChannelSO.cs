using UnityEngine;
using UnityEngine.Events;

namespace LudumDare.Template.Events
{
    [CreateAssetMenu(menuName = "LudumDare/Events/Int Event Channel", fileName = "IntEventChannel")]
    public class IntEventChannelSO : ScriptableObject
    {
        public UnityAction<int> OnEventRaised;

        public void Raise(int value) => OnEventRaised?.Invoke(value);
    }
}
