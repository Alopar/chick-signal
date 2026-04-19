using UnityEngine;
using UnityEngine.Audio;

namespace LudumDare.Template.Managers
{
    [CreateAssetMenu(menuName = "LudumDare/Audio/Audio Cue", fileName = "AudioCue")]
    public class AudioCueSO : ScriptableObject
    {
        public AudioClip[] Clips;

        [Range(0f, 1f)] public float Volume = 1f;
        public Vector2 PitchRange = new Vector2(1f, 1f);

        public bool Loop;
        public AudioMixerGroup MixerGroup;

        public AudioClip PickClip()
        {
            if (Clips == null || Clips.Length == 0) return null;
            return Clips[Random.Range(0, Clips.Length)];
        }

        public float PickPitch() => Random.Range(PitchRange.x, PitchRange.y);
    }
}
