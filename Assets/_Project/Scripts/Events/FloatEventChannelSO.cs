using UnityEngine;
using UnityEngine.Events;

namespace LudumDare.Template.Events
{
    [CreateAssetMenu(menuName = "LudumDare/Events/Float Event Channel", fileName = "FloatEventChannel")]
    public class FloatEventChannelSO : ScriptableObject
    {
        public UnityAction<float> OnEventRaised;

        public void Raise(float value) => OnEventRaised?.Invoke(value);
    }
}
