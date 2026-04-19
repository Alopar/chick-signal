using UnityEngine;
using UnityEngine.Events;

namespace LudumDare.Template.Events
{
    [CreateAssetMenu(menuName = "LudumDare/Events/String Event Channel", fileName = "StringEventChannel")]
    public class StringEventChannelSO : ScriptableObject
    {
        public UnityAction<string> OnEventRaised;

        public void Raise(string value) => OnEventRaised?.Invoke(value);
    }
}
