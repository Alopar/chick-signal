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
            if (Object.FindAnyObjectByType<SignalGameController>() != null) return;

            var go = new GameObject("SignalGameplay");
            go.AddComponent<SignalGameController>();
            go.AddComponent<SignalGameplayView>();
            go.AddComponent<SignalHudPresenter>();
            go.AddComponent<SignalEvolutionPanel>();
        }
    }
}
