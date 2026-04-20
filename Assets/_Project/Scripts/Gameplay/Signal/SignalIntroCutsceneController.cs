using System.Collections;
using UnityEngine;

namespace LudumDare.Template.Gameplay.Signal
{
    /// <summary>
    /// В сцене игры: до окончания вводной анимации отключает игровой SIGNAL, игрока и опционально отдельный HUD.
    /// Автозагрузка SIGNAL должна создавать неактивный корень: используется флаг и/или наличие этого компонента в сцене
    /// (т.к. RuntimeInitialize AfterSceneLoad может выполниться раньше Awake).
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public sealed class SignalIntroCutsceneController : MonoBehaviour
    {
        [SerializeField] private Animator _cutsceneAnimator;
        [Tooltip("Альтернатива Animator: покадровое воспроизведение (префаб BirdBornCutscene с заполненными кадрами).")]
        [SerializeField] private BirdBornCutsceneSpritePlayer _spriteCutscene;
        [Tooltip("Корень визуала вводной сцены (скрывается после окончания). Если не задан — берётся Animator или Sprite Player.")]
        [SerializeField] private GameObject _cutsceneVisualRoot;
        [Tooltip("Игрок в сцене (обычно с тегом Player). До конца вводной сцены отключается.")]
        [SerializeField] private GameObject _playerObject;
        [Tooltip("Дополнительный корень HUD, если он не на SignalGameplay (иначе можно не задавать).")]
        [SerializeField] private GameObject _extraHudRoot;
        [SerializeField] private bool _skipIntro;

        private bool _finished;

        private void Awake()
        {
            if (_skipIntro)
                return;

            SignalGameplayBootstrapDefer.RequestDeferGameplayStart();
            // На случай, если SignalGameplay уже успел создаться/быть активен до этого Awake (порядок загрузки / префаб в сцене).
            DeactivateAnyActiveSignalGameplayRoots();
        }

        private static void DeactivateAnyActiveSignalGameplayRoots()
        {
            var ctrls = Object.FindObjectsByType<SignalGameController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (ctrls == null)
                return;
            for (int i = 0; i < ctrls.Length; i++)
            {
                if (ctrls[i] != null && ctrls[i].gameObject.activeSelf)
                    ctrls[i].gameObject.SetActive(false);
            }
        }

        /// <summary>Для автозагрузки: объект с интро уже есть в загруженной сцене до Awake.</summary>
        public static bool SceneContainsIntroController()
        {
            var intros = Object.FindObjectsByType<SignalIntroCutsceneController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            return intros != null && intros.Length > 0;
        }

        private void Start()
        {
            if (_skipIntro)
            {
                if (_cutsceneVisualRoot != null)
                    _cutsceneVisualRoot.SetActive(false);
                else if (_spriteCutscene != null)
                    _spriteCutscene.gameObject.SetActive(false);
                else if (_cutsceneAnimator != null)
                    _cutsceneAnimator.gameObject.SetActive(false);
                EnsureGameplayAndPlayerActive();
                _finished = true;
                return;
            }

            DeactivateAnyActiveSignalGameplayRoots();

            if (_playerObject == null)
            {
                var tagged = GameObject.FindGameObjectWithTag("Player");
                if (tagged != null)
                    _playerObject = tagged;
            }

            if (_playerObject != null)
                _playerObject.SetActive(false);

            if (_extraHudRoot != null)
                _extraHudRoot.SetActive(false);

            ResolveCutsceneReferences();

            if (_spriteCutscene != null && _spriteCutscene.HasFrames)
            {
                if (_cutsceneVisualRoot == null)
                    _cutsceneVisualRoot = _spriteCutscene.gameObject;
                _spriteCutscene.Finished += OnSpriteCutsceneFinished;
                _spriteCutscene.PlayFromStart();
                return;
            }

            if (_cutsceneVisualRoot == null && _cutsceneAnimator != null)
                _cutsceneVisualRoot = _cutsceneAnimator.gameObject;

            if (_cutsceneAnimator == null && _cutsceneVisualRoot != null)
                _cutsceneAnimator = _cutsceneVisualRoot.GetComponentInChildren<Animator>(true);

            if (_cutsceneAnimator != null)
            {
                _cutsceneAnimator.enabled = true;
                _cutsceneAnimator.Rebind();
                _cutsceneAnimator.Update(0f);
                _cutsceneAnimator.Play(0, 0, 0f);
                StartCoroutine(WaitCutsceneEnd());
            }
            else
            {
                Debug.LogWarning(
                    "[SignalIntroCutscene] Нет ни Animator, ни BirdBornCutsceneSpritePlayer с кадрами — вводная сцена завершается сразу. " +
                    "Добавьте префаб BirdBornCutscene или назначьте компоненты.");
                FinishIntro();
            }
        }

        private void OnDestroy()
        {
            if (_spriteCutscene != null)
                _spriteCutscene.Finished -= OnSpriteCutsceneFinished;
        }

        private void ResolveCutsceneReferences()
        {
            if (_spriteCutscene == null && _cutsceneVisualRoot != null)
                _spriteCutscene = _cutsceneVisualRoot.GetComponent<BirdBornCutsceneSpritePlayer>();
            if (_spriteCutscene == null && _cutsceneAnimator != null)
                _spriteCutscene = _cutsceneAnimator.GetComponent<BirdBornCutsceneSpritePlayer>();
        }

        private void OnSpriteCutsceneFinished()
        {
            if (_spriteCutscene != null)
                _spriteCutscene.Finished -= OnSpriteCutsceneFinished;
            FinishIntro();
        }

        private IEnumerator WaitCutsceneEnd()
        {
            yield return null;
            yield return null;

            float guard = 0f;
            const float maxWait = 600f;

            while (!_finished && guard < maxWait)
            {
                guard += Time.deltaTime;
                var info = _cutsceneAnimator.GetCurrentAnimatorStateInfo(0);
                if (info.length > 0.001f && !info.loop && info.normalizedTime >= 1f && !_cutsceneAnimator.IsInTransition(0))
                {
                    FinishIntro();
                    yield break;
                }

                yield return null;
            }

            if (!_finished)
                FinishIntro();
        }

        private void FinishIntro()
        {
            if (_finished)
                return;
            _finished = true;

            if (_cutsceneVisualRoot != null)
                _cutsceneVisualRoot.SetActive(false);
            else if (_spriteCutscene != null)
                _spriteCutscene.gameObject.SetActive(false);
            else if (_cutsceneAnimator != null)
                _cutsceneAnimator.gameObject.SetActive(false);

            EnsureGameplayAndPlayerActive();
        }

        private void EnsureGameplayAndPlayerActive()
        {
            var gameplay = FindSignalGameplayRoot();
            if (gameplay != null && !gameplay.activeSelf)
                gameplay.SetActive(true);

            if (_extraHudRoot != null)
                _extraHudRoot.SetActive(true);

            if (_playerObject != null)
                _playerObject.SetActive(true);
        }

        private static GameObject FindSignalGameplayRoot()
        {
            var found = Object.FindObjectsByType<SignalGameController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            return found != null && found.Length > 0 ? found[0].gameObject : null;
        }
    }
}
