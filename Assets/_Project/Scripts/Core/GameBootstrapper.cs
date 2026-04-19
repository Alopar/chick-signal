using LudumDare.Template.Managers;
using UnityEngine;

namespace LudumDare.Template.Core
{
    /// <summary>
    /// Lives in <c>00_Bootstrap</c>. Instantiates the persistent Managers prefab (if not already alive)
    /// and hands off to the main menu. Keep this scene minimal — no gameplay objects.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class GameBootstrapper : MonoBehaviour
    {
        [SerializeField] private GameObject _managersPrefab;
        [SerializeField] private bool _loadMainMenu = true;

        private void Awake()
        {
            if (!Singleton<GameManager>.HasInstance && _managersPrefab != null)
            {
                var managers = Instantiate(_managersPrefab);
                managers.name = _managersPrefab.name;
                DontDestroyOnLoad(managers);
            }
        }

        private void Start()
        {
            if (GameManager.HasInstance)
            {
                GameManager.Instance.SetState(GameState.MainMenu);
            }

            if (_loadMainMenu && SceneLoader.HasInstance)
            {
                SceneLoader.Instance.LoadMainMenu();
            }
        }
    }
}
