using System;
using UnityEngine;

namespace LudumDare.Template.Managers
{
    [CreateAssetMenu(menuName = "LudumDare/Audio/Scene Music Config", fileName = "SceneMusicConfig")]
    public class SceneMusicConfigSO : ScriptableObject
    {
        [Serializable]
        public struct SceneMusicEntry
        {
            public string SceneName;
            public AudioCueSO Cue;
            [Min(0f)] public float FadeSeconds;
        }

        [Header("Scene Rules")]
        [SerializeField] private SceneMusicEntry[] _entries = Array.Empty<SceneMusicEntry>();

        [Header("Fallback")]
        [SerializeField] private AudioCueSO _defaultCue;
        [Min(0f)]
        [SerializeField] private float _defaultFadeSeconds = 1f;
        [SerializeField] private bool _silenceIfNotMapped;

        public bool TryGetForScene(string sceneName, out AudioCueSO cue, out float fadeSeconds)
        {
            if (!string.IsNullOrWhiteSpace(sceneName))
            {
                for (int i = 0; i < _entries.Length; i++)
                {
                    var entry = _entries[i];
                    if (!string.Equals(entry.SceneName, sceneName, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    cue = entry.Cue;
                    fadeSeconds = entry.FadeSeconds;
                    return true;
                }
            }

            if (_defaultCue != null)
            {
                cue = _defaultCue;
                fadeSeconds = _defaultFadeSeconds;
                return true;
            }

            cue = null;
            fadeSeconds = _defaultFadeSeconds;
            return false;
        }

        public bool SilenceIfNotMapped => _silenceIfNotMapped;
    }
}
