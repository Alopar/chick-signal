using System.Collections;
using System.Collections.Generic;
using LudumDare.Template.Core;
using UnityEngine;
using UnityEngine.Audio;

namespace LudumDare.Template.Managers
{
    /// <summary>
    /// Pooled SFX + crossfading music. Reads saved volumes from <see cref="SaveManager"/> on start and
    /// pushes them into the AudioMixer (exposed params: Master, Music, SFX — same names as the mixer groups, in dB).
    /// </summary>
    public class AudioManager : Singleton<AudioManager>
    {
        [Header("Mixer")]
        [SerializeField] private AudioMixer _mixer;
        [SerializeField] private AudioMixerGroup _musicGroup;
        [SerializeField] private AudioMixerGroup _sfxGroup;

        [Header("Pool")]
        [SerializeField] private int _sfxPoolSize = 12;

        private readonly Queue<AudioSource> _sfxPool = new();
        private AudioSource _musicA;
        private AudioSource _musicB;
        private bool _usingA = true;

        private AudioSource _heldSfx;
        private AudioCueSO _heldCueActive;

        private AudioSource _nestFeedingSfx;

        private const string ParamMaster = "MasterVolume";
        private const string ParamMusic  = "MusicVolume";
        private const string ParamSfx    = "SFXVolume";

        protected override void OnAwakeSingleton()
        {
            _musicA = CreateSource("Music_A", _musicGroup, loop: true);
            _musicB = CreateSource("Music_B", _musicGroup, loop: true);

            for (int i = 0; i < _sfxPoolSize; i++)
            {
                _sfxPool.Enqueue(CreateSource($"SFX_{i}", _sfxGroup, loop: false));
            }
        }

        private void Start()
        {
            if (SaveManager.HasInstance)
            {
                ApplyVolumes(SaveManager.Instance.MasterVolume, SaveManager.Instance.MusicVolume, SaveManager.Instance.SfxVolume);
            }
        }

        private AudioSource CreateSource(string sourceName, AudioMixerGroup group, bool loop)
        {
            var go = new GameObject(sourceName);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = loop;
            src.spatialBlend = 0f;
            src.outputAudioMixerGroup = group;
            return src;
        }

        public void ApplyVolumes(float master, float music, float sfx)
        {
            if (_mixer == null) return;
            _mixer.SetFloat(ParamMaster, LinearToDb(master));
            _mixer.SetFloat(ParamMusic,  LinearToDb(music));
            _mixer.SetFloat(ParamSfx,    LinearToDb(sfx));
        }

        public void SetMasterVolume(float v) { if (_mixer != null) _mixer.SetFloat(ParamMaster, LinearToDb(v)); if (SaveManager.HasInstance) SaveManager.Instance.MasterVolume = v; }
        public void SetMusicVolume(float v)  { if (_mixer != null) _mixer.SetFloat(ParamMusic,  LinearToDb(v)); if (SaveManager.HasInstance) SaveManager.Instance.MusicVolume  = v; }
        public void SetSfxVolume(float v)    { if (_mixer != null) _mixer.SetFloat(ParamSfx,    LinearToDb(v)); if (SaveManager.HasInstance) SaveManager.Instance.SfxVolume    = v; }

        private static float LinearToDb(float linear) => linear <= 0.0001f ? -80f : Mathf.Log10(Mathf.Clamp01(linear)) * 20f;

        public void PlaySFX(AudioCueSO cue)
        {
            if (cue == null) return;
            var clip = cue.PickClip();
            if (clip == null) return;

            var src = _sfxPool.Dequeue();
            src.clip = clip;
            src.volume = cue.Volume;
            src.pitch = cue.PickPitch();
            src.outputAudioMixerGroup = cue.MixerGroup != null ? cue.MixerGroup : _sfxGroup;
            src.Play();
            _sfxPool.Enqueue(src);
        }

        /// <summary>
        /// Один удерживаемый зацикленный SFX (например сигнал/контрсигнал). При смене cue или <c>null</c> — остановка/перезапуск.
        /// </summary>
        public void SetHeldLoopingSfx(AudioCueSO cue)
        {
            if (cue == null)
            {
                if (_heldSfx != null) _heldSfx.Stop();
                _heldCueActive = null;
                return;
            }

            if (_heldCueActive == cue && _heldSfx != null && _heldSfx.isPlaying)
                return;

            if (cue.Clips == null || cue.Clips.Length == 0) return;
            var clip = cue.Clips[0];
            if (clip == null) return;

            if (_heldSfx == null)
            {
                _heldSfx = CreateSource("SFX_Held", _sfxGroup, loop: true);
            }

            _heldCueActive = cue;
            _heldSfx.clip = clip;
            _heldSfx.volume = cue.Volume;
            _heldSfx.pitch = 0.5f * (cue.PitchRange.x + cue.PitchRange.y);
            _heldSfx.loop = cue.Loop;
            _heldSfx.outputAudioMixerGroup = cue.MixerGroup != null ? cue.MixerGroup : _sfxGroup;
            _heldSfx.Play();
        }

        /// <summary>
        /// Зацикленный SFX режима кормления гнезда. Каждый вызов перезапускает клип с начала (новая волна).
        /// </summary>
        public void PlayNestFeedingWaveLoop(AudioCueSO cue)
        {
            if (cue == null) return;
            if (cue.Clips == null || cue.Clips.Length == 0) return;
            var clip = cue.Clips[0];
            if (clip == null) return;

            if (_nestFeedingSfx == null)
                _nestFeedingSfx = CreateSource("SFX_NestFeeding", _sfxGroup, loop: true);

            _nestFeedingSfx.clip = clip;
            _nestFeedingSfx.volume = cue.Volume;
            _nestFeedingSfx.pitch = 0.5f * (cue.PitchRange.x + cue.PitchRange.y);
            _nestFeedingSfx.loop = cue.Loop;
            _nestFeedingSfx.outputAudioMixerGroup = cue.MixerGroup != null ? cue.MixerGroup : _sfxGroup;
            _nestFeedingSfx.time = 0f;
            _nestFeedingSfx.Play();
        }

        public void StopNestFeedingLoop()
        {
            if (_nestFeedingSfx != null) _nestFeedingSfx.Stop();
        }

        public void PlayMusic(AudioCueSO cue, float fadeSeconds = 1f)
        {
            if (cue == null) return;
            var clip = cue.PickClip();
            if (clip == null) return;

            var next = _usingA ? _musicB : _musicA;
            var current = _usingA ? _musicA : _musicB;
            _usingA = !_usingA;

            next.clip = clip;
            next.volume = 0f;
            next.outputAudioMixerGroup = cue.MixerGroup != null ? cue.MixerGroup : _musicGroup;
            next.loop = cue.Loop;
            next.Play();

            StartCoroutine(CrossfadeRoutine(current, next, cue.Volume, fadeSeconds));
        }

        public void StopMusic(float fadeSeconds = 1f)
        {
            var current = _usingA ? _musicA : _musicB;
            StartCoroutine(FadeOutRoutine(current, fadeSeconds));
        }

        private IEnumerator CrossfadeRoutine(AudioSource from, AudioSource to, float targetVolume, float duration)
        {
            float t = 0f;
            float fromStart = from != null ? from.volume : 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = duration <= 0f ? 1f : Mathf.Clamp01(t / duration);
                if (from != null) from.volume = Mathf.Lerp(fromStart, 0f, k);
                to.volume = Mathf.Lerp(0f, targetVolume, k);
                yield return null;
            }
            if (from != null) from.Stop();
        }

        private IEnumerator FadeOutRoutine(AudioSource src, float duration)
        {
            if (src == null) yield break;
            float start = src.volume;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                src.volume = Mathf.Lerp(start, 0f, Mathf.Clamp01(t / duration));
                yield return null;
            }
            src.Stop();
        }
    }
}
