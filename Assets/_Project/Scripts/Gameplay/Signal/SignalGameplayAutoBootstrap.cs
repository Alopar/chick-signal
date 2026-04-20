using LudumDare.Template.Managers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LudumDare.Template.Gameplay.Signal
{
    /// <summary>
    /// В сцене игры создаёт корень SIGNAL с контроллером и UI, если их ещё нет.
    /// </summary>
    public static class SignalGameplayAutoBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (SceneManager.GetActiveScene().name != SceneLoader.GameScene) return;
            var existing = Object.FindObjectsByType<SignalGameController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (existing != null && existing.Length > 0)
                return;

            // AfterSceneLoad иногда выполняется раньше Awake: флаг из интро ещё не выставлен — ищем компонент в сцене.
            bool defer = SignalGameplayBootstrapDefer.DeferGameplayStartRequested
                || SignalIntroCutsceneController.SceneContainsIntroController();
            SignalGameplayBootstrapDefer.ClearDeferRequest();

            var go = new GameObject("SignalGameplay");
            if (defer)
                go.SetActive(false);

            go.AddComponent<SignalGameController>();
            go.AddComponent<SignalGameplayView>();
            go.AddComponent<SignalHudPresenter>();
        }
    }
}
