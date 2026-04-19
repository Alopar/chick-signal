using UnityEngine;
using UnityEngine.SceneManagement;

namespace LudumDare.Template.Managers
{
    public class MusicDirector : MonoBehaviour
    {
        [SerializeField] private SceneMusicConfigSO _sceneMusicConfig;
        [SerializeField] private bool _applyOnStart = true;

        private AudioCueSO _activeCue;
        private string _activeSceneName;

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void Start()
        {
            if (_applyOnStart)
            {
                ApplyForScene(SceneManager.GetActiveScene().name);
            }
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyForScene(scene.name);
        }

        private void ApplyForScene(string sceneName)
        {
            if (!AudioManager.HasInstance)
            {
                return;
            }

            if (_sceneMusicConfig == null)
            {
                return;
            }

            bool hasMapping = _sceneMusicConfig.TryGetForScene(sceneName, out var cue, out var fadeSeconds);
            if (!hasMapping)
            {
                if (_sceneMusicConfig.SilenceIfNotMapped)
                {
                    AudioManager.Instance.StopMusic(1f);
                    _activeCue = null;
                    _activeSceneName = sceneName;
                }
                return;
            }

            if (_activeCue == cue && _activeSceneName == sceneName)
            {
                return;
            }

            if (cue == null)
            {
                AudioManager.Instance.StopMusic(fadeSeconds);
            }
            else
            {
                AudioManager.Instance.PlayMusic(cue, fadeSeconds);
            }

            _activeCue = cue;
            _activeSceneName = sceneName;
        }
    }
}
