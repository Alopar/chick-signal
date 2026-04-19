using LudumDare.Template.Core;
using UnityEngine;

namespace LudumDare.Template.Managers
{
    /// <summary>
    /// Thin wrapper around PlayerPrefs. All jam-level persistence (volumes, best score, settings) goes through here.
    /// </summary>
    public class SaveManager : Singleton<SaveManager>
    {
        private const string KeyMasterVolume = "audio.master";
        private const string KeyMusicVolume  = "audio.music";
        private const string KeySfxVolume    = "audio.sfx";
        private const string KeyFullscreen   = "video.fullscreen";
        private const string KeyBestScore    = "gameplay.bestScore";
        private const string KeyLastNickname = "gameplay.lastNickname";

        public float MasterVolume
        {
            get => PlayerPrefs.GetFloat(KeyMasterVolume, 1f);
            set { PlayerPrefs.SetFloat(KeyMasterVolume, Mathf.Clamp01(value)); PlayerPrefs.Save(); }
        }

        public float MusicVolume
        {
            get => PlayerPrefs.GetFloat(KeyMusicVolume, 0.8f);
            set { PlayerPrefs.SetFloat(KeyMusicVolume, Mathf.Clamp01(value)); PlayerPrefs.Save(); }
        }

        public float SfxVolume
        {
            get => PlayerPrefs.GetFloat(KeySfxVolume, 1f);
            set { PlayerPrefs.SetFloat(KeySfxVolume, Mathf.Clamp01(value)); PlayerPrefs.Save(); }
        }

        public bool Fullscreen
        {
            get => PlayerPrefs.GetInt(KeyFullscreen, 1) == 1;
            set { PlayerPrefs.SetInt(KeyFullscreen, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public int BestScore
        {
            get => PlayerPrefs.GetInt(KeyBestScore, 0);
            set { PlayerPrefs.SetInt(KeyBestScore, value); PlayerPrefs.Save(); }
        }

        public bool TrySetBestScore(int candidate)
        {
            if (candidate <= BestScore) return false;
            BestScore = candidate;
            return true;
        }

        public string LastNickname
        {
            get => PlayerPrefs.GetString(KeyLastNickname, string.Empty);
            set
            {
                if (string.IsNullOrEmpty(value)) PlayerPrefs.DeleteKey(KeyLastNickname);
                else PlayerPrefs.SetString(KeyLastNickname, value);
                PlayerPrefs.Save();
            }
        }
    }
}
