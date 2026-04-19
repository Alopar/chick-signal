#if UNITY_EDITOR
using System.IO;
using LudumDare.Template.Core;
using LudumDare.Template.Events;
using LudumDare.Template.Gameplay.Player;
using LudumDare.Template.Input;
using LudumDare.Template.Managers;
using LudumDare.Template.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LudumDare.Template.EditorTools
{
    public static class TemplateSceneSetup
    {
        private const string ManagersPrefabPath = "Assets/_Project/Prefabs/Managers/_Managers.prefab";
        private const string UIRootPrefabPath   = "Assets/_Project/Prefabs/UI/UIRoot.prefab";
        private const string PlayerPrefabPath   = "Assets/_Project/Prefabs/Gameplay/Player.prefab";
        private const string NicknameEntryScreenPrefabPath = "Assets/_Project/Prefabs/UI/Screens/MainMenu/NicknameEntryScreen.prefab";
        private const string LeaderboardScreenPrefabPath = "Assets/_Project/Prefabs/UI/Screens/MainMenu/LeaderboardScreen.prefab";
        private const string LeaderboardConfigPath = "Assets/_Project/Settings/LeaderboardConfig.asset";

        private const string BootstrapScenePath = "Assets/_Project/Scenes/00_Bootstrap.unity";
        private const string MainMenuScenePath  = "Assets/_Project/Scenes/01_MainMenu.unity";
        private const string GameScenePath      = "Assets/_Project/Scenes/02_Game.unity";

        private const string VolumeProfilePath = "Assets/_Project/Settings/PostProcessing/GlobalVolumeProfile.asset";

        [MenuItem("Tools/LudumDare/Regenerate Nickname + Leaderboard Prefabs")]
        public static void RegenerateNicknameLeaderboardPrefabsOnly()
        {
            EnsureFolders();
            BuildNicknameAndLeaderboardScreenPrefabs();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[TemplateSceneSetup] Saved: " + NicknameEntryScreenPrefabPath + ", " + LeaderboardScreenPrefabPath);
        }

        /// <summary>
        /// Places Nickname + Leaderboard prefabs under UIRoot and assigns references on MainMenu / EndGame without rebuilding the whole template.
        /// </summary>
        [MenuItem("Tools/LudumDare/Connect Nickname + Leaderboard Prefabs To Scenes")]
        public static void ConnectNicknameLeaderboardPrefabsToScenes()
        {
            EnsureFolders();
            if (!File.Exists(NicknameEntryScreenPrefabPath) || !File.Exists(LeaderboardScreenPrefabPath))
                BuildNicknameAndLeaderboardScreenPrefabs();

            ConnectScreensInScene(MainMenuScenePath, wireEndGame: false);
            ConnectScreensInScene(GameScenePath, wireEndGame: true);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[TemplateSceneSetup] Nickname + Leaderboard prefabs connected in 01_MainMenu and 02_Game.");
        }

        [MenuItem("Tools/LudumDare/Build Template (Prefabs + Scenes)")]
        public static void BuildTemplate()
        {
            EnsureFolders();
            EnsureLeaderboardConfig();
            BuildNicknameAndLeaderboardScreenPrefabs();

            var managersPrefab = BuildManagersPrefab();
            var uiRootPrefab   = BuildUIRootPrefab();
            var playerPrefab   = BuildPlayerPrefab();

            BuildBootstrapScene(managersPrefab);
            BuildMainMenuScene(uiRootPrefab);
            BuildGameScene(uiRootPrefab, playerPrefab);

            AddScenesToBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[TemplateSceneSetup] Template built. Open 00_Bootstrap to play.");
        }

        private static void EnsureFolders()
        {
            foreach (var f in new[] {
                "Assets/_Project/Prefabs/Managers",
                "Assets/_Project/Prefabs/UI",
                "Assets/_Project/Prefabs/UI/Screens",
                "Assets/_Project/Prefabs/UI/Screens/MainMenu",
                "Assets/_Project/Prefabs/Gameplay",
                "Assets/_Project/Scenes",
                "Assets/_Project/Settings",
            })
            {
                if (!AssetDatabase.IsValidFolder(f))
                {
                    var parent = Path.GetDirectoryName(f)!.Replace('\\', '/');
                    var leaf = Path.GetFileName(f);
                    AssetDatabase.CreateFolder(parent, leaf);
                }
            }
        }

        private static T LoadByName<T>(string assetName) where T : UnityEngine.Object
        {
            var guids = AssetDatabase.FindAssets($"{assetName} t:{typeof(T).Name}");
            foreach (var guid in guids)
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null && asset.name == assetName) return asset;
            }
            return null;
        }

        private static GameObject BuildManagersPrefab()
        {
            var root = new GameObject("_Managers");

            var game = new GameObject("GameManager"); game.transform.SetParent(root.transform, false);
            var gm = game.AddComponent<GameManager>();
            SetField(gm, "_stateChannel", LoadByName<GameStateEventChannelSO>("OnGameStateChanged"));
            SetField(gm, "_scoreChannel", LoadByName<IntEventChannelSO>("OnScoreChanged"));

            var save = new GameObject("SaveManager"); save.transform.SetParent(root.transform, false);
            save.AddComponent<SaveManager>();

            var pause = new GameObject("PauseService"); pause.transform.SetParent(root.transform, false);
            var ps = pause.AddComponent<PauseService>();
            SetField(ps, "_onPaused", LoadByName<VoidEventChannelSO>("OnGamePaused"));
            SetField(ps, "_onResumed", LoadByName<VoidEventChannelSO>("OnGameResumed"));

            var scene = new GameObject("SceneLoader"); scene.transform.SetParent(root.transform, false);
            var sl = scene.AddComponent<SceneLoader>();
            SetField(sl, "_onLoadStart", LoadByName<VoidEventChannelSO>("OnLoadStart"));
            SetField(sl, "_onLoadComplete", LoadByName<VoidEventChannelSO>("OnLoadComplete"));

            var audio = new GameObject("AudioManager"); audio.transform.SetParent(root.transform, false);
            audio.AddComponent<AudioListener>();
            var am = audio.AddComponent<AudioManager>();
            var mixer = AssetDatabase.LoadAssetAtPath<UnityEngine.Audio.AudioMixer>("Assets/_Project/Audio/Mixers/MainMixer.mixer");
            if (mixer != null)
            {
                SetField(am, "_mixer", mixer);
                var groups = mixer.FindMatchingGroups("Music");
                if (groups.Length > 0) SetField(am, "_musicGroup", groups[0]);
                groups = mixer.FindMatchingGroups("SFX");
                if (groups.Length > 0) SetField(am, "_sfxGroup", groups[0]);
            }

            var ui = new GameObject("UIManager"); ui.transform.SetParent(root.transform, false);
            ui.AddComponent<UIManager>();

            var lbGo = new GameObject("LeaderboardClient"); lbGo.transform.SetParent(root.transform, false);
            var lb = lbGo.AddComponent<LeaderboardClient>();
            var lbCfg = AssetDatabase.LoadAssetAtPath<LeaderboardConfig>(LeaderboardConfigPath);
            SetField(lb, "_config", lbCfg);

            var psGo = new GameObject("PlayerSession"); psGo.transform.SetParent(root.transform, false);
            psGo.AddComponent<PlayerSession>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, ManagersPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject BuildUIRootPrefab()
        {
            var root = new GameObject("UIRoot");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, UIRootPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>
        /// Standalone nickname and leaderboard screen prefabs (for dragging from Project into scenes).
        /// Template scenes still build the same objects via <see cref="BuildMainMenuScene"/> / <see cref="BuildGameScene"/>.
        /// </summary>
        private static void BuildNicknameAndLeaderboardScreenPrefabs()
        {
            var root = new GameObject("UIRoot_PrefabGen");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();

            var nickname = BuildNicknameEntryUI(root.transform);
            var leaderboard = BuildLeaderboardUI(root.transform);
            nickname.SetActive(false);
            leaderboard.SetActive(false);

            PrefabUtility.SaveAsPrefabAsset(nickname, NicknameEntryScreenPrefabPath);
            PrefabUtility.SaveAsPrefabAsset(leaderboard, LeaderboardScreenPrefabPath);

            Object.DestroyImmediate(root);
        }

        private static GameObject BuildPlayerPrefab()
        {
            var go = new GameObject("Player");
            go.tag = "Player";
            go.layer = LayerMask.NameToLayer("Player");
            go.AddComponent<SpriteRenderer>();
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.freezeRotation = true;
            go.AddComponent<BoxCollider2D>();
            var controller = go.AddComponent<PlayerController>();
            SetField(controller, "_inputReader", LoadByName<InputReader>("InputReader"));
            SetField(controller, "_attackCue", LoadByName<AudioCueSO>("PlayerAttackCue"));

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PlayerPrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static void BuildBootstrapScene(GameObject managersPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var bootstrapGo = new GameObject("Bootstrap");
            var bootstrap = bootstrapGo.AddComponent<GameBootstrapper>();
            SetField(bootstrap, "_managersPrefab", managersPrefab);

            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));

            EditorSceneManager.SaveScene(scene, BootstrapScenePath);
        }

        private static void BuildMainMenuScene(GameObject uiRootPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cam = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.08f, 0.1f, 1f);
            cam.orthographic = true;
            cam.orthographicSize = 5f;

            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));

            var uiRoot = (GameObject)PrefabUtility.InstantiatePrefab(uiRootPrefab);
            uiRoot.name = "UIRoot";

            var mainMenu = BuildMainMenuUI(uiRoot.transform);
            var settings = BuildSettingsUI(uiRoot.transform);
            var nickname = InstantiateScreenPrefabOrFallback(uiRoot.transform, NicknameEntryScreenPrefabPath, BuildNicknameEntryUI);
            var leaderboard = InstantiateScreenPrefabOrFallback(uiRoot.transform, LeaderboardScreenPrefabPath, BuildLeaderboardUI);

            var mainMenuScreen = mainMenu.GetComponent<MainMenuScreen>();
            SetField(mainMenuScreen, "_settingsScreen", settings.GetComponent<SettingsScreen>());
            SetField(mainMenuScreen, "_nicknameEntryScreen", nickname.GetComponent<NicknameEntryScreen>());
            SetField(mainMenuScreen, "_leaderboardScreen", leaderboard.GetComponent<LeaderboardScreen>());

            mainMenu.SetActive(true);
            var cg = mainMenu.GetComponent<CanvasGroup>();
            cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true;

            EditorSceneManager.SaveScene(scene, MainMenuScenePath);
        }

        private static void BuildGameScene(GameObject uiRootPrefab, GameObject playerPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cam = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
            cam.tag = "MainCamera";
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.07f, 1f);
            cam.clearFlags = CameraClearFlags.SolidColor;

            var volumeGo = new GameObject("Global Volume");
            var volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 0f;
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile != null) volume.sharedProfile = profile;

            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));

            new GameObject("GameRoot");

            if (playerPrefab != null)
            {
                var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
                player.transform.position = Vector3.zero;
            }

            var uiRoot = (GameObject)PrefabUtility.InstantiatePrefab(uiRootPrefab);
            uiRoot.name = "UIRoot";

            var hud = BuildHUD(uiRoot.transform);
            var pause = BuildPauseUI(uiRoot.transform);
            var leaderboard = InstantiateScreenPrefabOrFallback(uiRoot.transform, LeaderboardScreenPrefabPath, BuildLeaderboardUI);
            var endGame = BuildEndGameUI(uiRoot.transform, leaderboard.GetComponent<LeaderboardScreen>());

            hud.SetActive(true);
            var hudCg = hud.GetComponent<CanvasGroup>();
            hudCg.alpha = 1f; hudCg.interactable = true; hudCg.blocksRaycasts = true;

            var watcherGo = new GameObject("PauseInputWatcher");
            var watcher = watcherGo.AddComponent<PauseInputWatcher>();
            SetField(watcher, "_pauseScreen", pause.GetComponent<PauseScreen>());

            EditorSceneManager.SaveScene(scene, GameScenePath);
        }

        private static GameObject BuildMainMenuUI(Transform parent)
        {
            var panel = CreatePanel("MainMenuScreen", parent);
            panel.AddComponent<MainMenuScreen>();

            var title = CreateTmpText("Title", panel.transform, "LUDUM DARE TEMPLATE", 64);
            title.rectTransform.anchorMin = new Vector2(0.5f, 0.75f);
            title.rectTransform.anchorMax = new Vector2(0.5f, 0.9f);
            title.rectTransform.sizeDelta = new Vector2(900, 140);
            title.alignment = TextAlignmentOptions.Center;

            var play = CreateButton("PlayButton", panel.transform, "Play", new Vector2(0.5f, 0.62f));
            var leaderboardBtn = CreateButton("LeaderboardButton", panel.transform, "Leaderboard", new Vector2(0.5f, 0.49f));
            var settings = CreateButton("SettingsButton", panel.transform, "Settings", new Vector2(0.5f, 0.36f));
            var quit = CreateButton("QuitButton", panel.transform, "Quit", new Vector2(0.5f, 0.23f));

            var screen = panel.GetComponent<MainMenuScreen>();
            SetField(screen, "_playButton", play);
            SetField(screen, "_leaderboardButton", leaderboardBtn);
            SetField(screen, "_settingsButton", settings);
            SetField(screen, "_quitButton", quit);
            SetField(screen, "_showOnStart", true);
            SetField(screen, "_replacesStack", true);
            return panel;
        }

        private static GameObject BuildSettingsUI(Transform parent)
        {
            var panel = CreatePanel("SettingsScreen", parent);
            panel.AddComponent<SettingsScreen>();

            var title = CreateTmpText("Title", panel.transform, "SETTINGS", 48);
            title.rectTransform.anchorMin = new Vector2(0.5f, 0.82f);
            title.rectTransform.anchorMax = new Vector2(0.5f, 0.9f);
            title.rectTransform.sizeDelta = new Vector2(600, 80);
            title.alignment = TextAlignmentOptions.Center;

            var master     = CreateLabeledSlider("MasterSlider", panel.transform, "Master", new Vector2(0.5f, 0.70f));
            var music      = CreateLabeledSlider("MusicSlider",  panel.transform, "Music",  new Vector2(0.5f, 0.58f));
            var sfx        = CreateLabeledSlider("SfxSlider",    panel.transform, "SFX",    new Vector2(0.5f, 0.46f));
            var fullscreen = CreateToggle("FullscreenToggle",    panel.transform, "Fullscreen", new Vector2(0.5f, 0.34f));
            var back       = CreateButton("BackButton",          panel.transform, "Back",       new Vector2(0.5f, 0.18f));

            var screen = panel.GetComponent<SettingsScreen>();
            SetField(screen, "_masterSlider", master);
            SetField(screen, "_musicSlider", music);
            SetField(screen, "_sfxSlider", sfx);
            SetField(screen, "_fullscreenToggle", fullscreen);
            SetField(screen, "_backButton", back);
            return panel;
        }

        private static GameObject BuildHUD(Transform parent)
        {
            var panel = CreatePanel("HUDScreen", parent);
            panel.AddComponent<HUDScreen>();

            var score = CreateTmpText("ScoreLabel", panel.transform, "Score: 0", 42);
            score.rectTransform.anchorMin = new Vector2(0f, 1f);
            score.rectTransform.anchorMax = new Vector2(0f, 1f);
            score.rectTransform.pivot = new Vector2(0f, 1f);
            score.rectTransform.anchoredPosition = new Vector2(32, -32);
            score.rectTransform.sizeDelta = new Vector2(400, 60);
            score.alignment = TextAlignmentOptions.TopLeft;

            var screen = panel.GetComponent<HUDScreen>();
            SetField(screen, "_scoreLabel", score);
            SetField(screen, "_scoreChannel", LoadByName<IntEventChannelSO>("OnScoreChanged"));
            SetField(screen, "_showOnStart", true);
            SetField(screen, "_replacesStack", true);
            return panel;
        }

        private static GameObject BuildPauseUI(Transform parent)
        {
            var panel = CreatePanel("PauseScreen", parent, dim: true);
            panel.AddComponent<PauseScreen>();

            var title = CreateTmpText("Title", panel.transform, "PAUSED", 64);
            title.rectTransform.anchorMin = new Vector2(0.5f, 0.7f);
            title.rectTransform.anchorMax = new Vector2(0.5f, 0.82f);
            title.rectTransform.sizeDelta = new Vector2(600, 120);
            title.alignment = TextAlignmentOptions.Center;

            var resume   = CreateButton("ResumeButton",   panel.transform, "Resume",    new Vector2(0.5f, 0.55f));
            var restart  = CreateButton("RestartButton",  panel.transform, "Restart",   new Vector2(0.5f, 0.42f));
            var mainMenu = CreateButton("MainMenuButton", panel.transform, "Main Menu", new Vector2(0.5f, 0.29f));

            var screen = panel.GetComponent<PauseScreen>();
            SetField(screen, "_resumeButton", resume);
            SetField(screen, "_restartButton", restart);
            SetField(screen, "_mainMenuButton", mainMenu);
            return panel;
        }

        private static GameObject BuildEndGameUI(Transform parent, LeaderboardScreen leaderboardScreen)
        {
            var panel = CreatePanel("EndGameScreen", parent, dim: true);
            panel.AddComponent<EndGameScreen>();

            var title = CreateTmpText("Title", panel.transform, "GAME OVER", 72);
            title.rectTransform.anchorMin = new Vector2(0.5f, 0.65f);
            title.rectTransform.anchorMax = new Vector2(0.5f, 0.8f);
            title.rectTransform.sizeDelta = new Vector2(900, 140);
            title.alignment = TextAlignmentOptions.Center;

            var score = CreateTmpText("ScoreLabel", panel.transform, "Score: 0\nBest: 0", 36);
            score.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            score.rectTransform.anchorMax = new Vector2(0.5f, 0.62f);
            score.rectTransform.sizeDelta = new Vector2(600, 120);
            score.alignment = TextAlignmentOptions.Center;

            var restart = CreateButton("RestartButton", panel.transform, "Restart", new Vector2(0.5f, 0.38f));
            var leaderboardBtn = CreateButton("LeaderboardButton", panel.transform, "Leaderboard", new Vector2(0.5f, 0.28f));
            var mainMenu = CreateButton("MainMenuButton", panel.transform, "Main Menu", new Vector2(0.5f, 0.18f));

            var screen = panel.GetComponent<EndGameScreen>();
            SetField(screen, "_titleLabel", title);
            SetField(screen, "_scoreLabel", score);
            SetField(screen, "_restartButton", restart);
            SetField(screen, "_leaderboardButton", leaderboardBtn);
            SetField(screen, "_mainMenuButton", mainMenu);
            SetField(screen, "_leaderboardScreen", leaderboardScreen);
            SetField(screen, "_stateChannel", LoadByName<GameStateEventChannelSO>("OnGameStateChanged"));
            return panel;
        }

        private static void EnsureLeaderboardConfig()
        {
            if (File.Exists(LeaderboardConfigPath)) return;
            var cfg = ScriptableObject.CreateInstance<LeaderboardConfig>();
            AssetDatabase.CreateAsset(cfg, LeaderboardConfigPath);
        }

        private static GameObject BuildNicknameEntryUI(Transform parent)
        {
            var panel = CreatePanel("NicknameEntryScreen", parent, dim: true);
            panel.AddComponent<NicknameEntryScreen>();

            var title = CreateTmpText("Title", panel.transform, "NICKNAME", 48);
            title.rectTransform.anchorMin = new Vector2(0.5f, 0.78f);
            title.rectTransform.anchorMax = new Vector2(0.5f, 0.88f);
            title.rectTransform.sizeDelta = new Vector2(800, 80);
            title.alignment = TextAlignmentOptions.Center;

            var input = CreateTmpInputField("NicknameInput", panel.transform, new Vector2(0.5f, 0.58f));

            var confirm = CreateButton("ConfirmButton", panel.transform, "Play", new Vector2(0.5f, 0.42f));
            var back = CreateButton("BackButton", panel.transform, "Back", new Vector2(0.5f, 0.3f));

            var screen = panel.GetComponent<NicknameEntryScreen>();
            SetField(screen, "_inputField", input);
            SetField(screen, "_confirmButton", confirm);
            SetField(screen, "_backButton", back);
            return panel;
        }

        private static GameObject BuildLeaderboardUI(Transform parent)
        {
            var panel = CreatePanel("LeaderboardScreen", parent, dim: true);
            panel.AddComponent<LeaderboardScreen>();

            var title = CreateTmpText("Title", panel.transform, "LEADERBOARD", 48);
            title.rectTransform.anchorMin = new Vector2(0.5f, 0.88f);
            title.rectTransform.anchorMax = new Vector2(0.5f, 0.96f);
            title.rectTransform.sizeDelta = new Vector2(900, 72);
            title.alignment = TextAlignmentOptions.Center;

            var status = CreateTmpText("StatusLabel", panel.transform, "Loading...", 22);
            status.rectTransform.anchorMin = new Vector2(0.5f, 0.82f);
            status.rectTransform.anchorMax = new Vector2(0.5f, 0.86f);
            status.rectTransform.sizeDelta = new Vector2(920, 36);
            status.alignment = TextAlignmentOptions.Center;

            var back = CreateButton("BackButton", panel.transform, "Back", new Vector2(0.5f, 0.07f));

            var scrollRoot = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollRoot.transform.SetParent(panel.transform, false);
            var srt = (RectTransform)scrollRoot.transform;
            srt.anchorMin = new Vector2(0.08f, 0.14f);
            srt.anchorMax = new Vector2(0.92f, 0.8f);
            srt.offsetMin = Vector2.zero;
            srt.offsetMax = Vector2.zero;
            scrollRoot.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.3f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewport.transform.SetParent(scrollRoot.transform, false);
            var vpRt = (RectTransform)viewport.transform;
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = new Vector2(6, 6);
            vpRt.offsetMax = new Vector2(-6, -6);
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var cRt = (RectTransform)content.transform;
            cRt.anchorMin = new Vector2(0f, 1f);
            cRt.anchorMax = new Vector2(1f, 1f);
            cRt.pivot = new Vector2(0.5f, 1f);
            cRt.anchoredPosition = Vector2.zero;
            cRt.sizeDelta = new Vector2(0f, 0f);

            var vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.spacing = 4f;
            vlg.padding = new RectOffset(10, 10, 10, 10);

            var csf = content.GetComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollRoot.GetComponent<ScrollRect>();
            scroll.viewport = vpRt;
            scroll.content = cRt;
            scroll.horizontal = false;
            scroll.vertical = true;

            var rowProtoGo = new GameObject("RowPrototype", typeof(RectTransform));
            rowProtoGo.SetActive(false);
            rowProtoGo.transform.SetParent(panel.transform, false);
            var rowRt = (RectTransform)rowProtoGo.transform;
            rowRt.sizeDelta = new Vector2(100f, 34f);
            var rowTmp = rowProtoGo.AddComponent<TextMeshProUGUI>();
            rowTmp.fontSize = 26;
            rowTmp.color = Color.white;
            rowTmp.text = " ";

            var screen = panel.GetComponent<LeaderboardScreen>();
            SetField(screen, "_backButton", back);
            SetField(screen, "_statusLabel", status);
            SetField(screen, "_rowsRoot", cRt);
            SetField(screen, "_rowPrefab", rowTmp);
            return panel;
        }

        private static TMP_InputField CreateTmpInputField(string objectName, Transform parent, Vector2 anchor)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(520f, 56f);
            rt.anchoredPosition = Vector2.zero;
            go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);

            var textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            textArea.transform.SetParent(go.transform, false);
            var taRt = (RectTransform)textArea.transform;
            taRt.anchorMin = new Vector2(0.03f, 0.12f);
            taRt.anchorMax = new Vector2(0.97f, 0.88f);
            taRt.offsetMin = Vector2.zero;
            taRt.offsetMax = Vector2.zero;

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(textArea.transform, false);
            var textRt = (RectTransform)textGo.transform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(10f, 4f);
            textRt.offsetMax = new Vector2(-10f, -4f);
            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.fontSize = 28;
            text.color = Color.white;

            var placeholderGo = new GameObject("Placeholder", typeof(RectTransform));
            placeholderGo.transform.SetParent(textArea.transform, false);
            var phRt = (RectTransform)placeholderGo.transform;
            phRt.anchorMin = Vector2.zero;
            phRt.anchorMax = Vector2.one;
            phRt.offsetMin = new Vector2(10f, 4f);
            phRt.offsetMax = new Vector2(-10f, -4f);
            var ph = placeholderGo.AddComponent<TextMeshProUGUI>();
            ph.fontSize = 28;
            ph.color = new Color(1f, 1f, 1f, 0.35f);
            ph.text = "Enter nickname...";

            var input = go.AddComponent<TMP_InputField>();
            input.textViewport = taRt;
            input.textComponent = text;
            input.placeholder = ph;
            input.lineType = TMP_InputField.LineType.SingleLine;
            return input;
        }

        private static GameObject CreatePanel(string panelName, Transform parent, bool dim = false)
        {
            var go = new GameObject(panelName, typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            if (dim)
            {
                var bg = new GameObject("DimBackground", typeof(Image));
                bg.transform.SetParent(go.transform, false);
                var bgRt = (RectTransform)bg.transform;
                bgRt.anchorMin = Vector2.zero;
                bgRt.anchorMax = Vector2.one;
                bgRt.offsetMin = Vector2.zero;
                bgRt.offsetMax = Vector2.zero;
                bg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);
            }

            go.SetActive(false);
            return go;
        }

        private static TMP_Text CreateTmpText(string textName, Transform parent, string content, float size)
        {
            var go = new GameObject(textName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = size;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            return text;
        }

        private static Button CreateButton(string objectName, Transform parent, string label, Vector2 anchor)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(360, 80);
            rt.anchoredPosition = Vector2.zero;
            go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);

            var labelText = CreateTmpText("Label", go.transform, label, 32);
            var labelRt = labelText.rectTransform;
            labelRt.anchorMin = Vector2.zero; labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero; labelRt.offsetMax = Vector2.zero;

            return go.GetComponent<Button>();
        }

        private static Slider CreateLabeledSlider(string objectName, Transform parent, string label, Vector2 anchor)
        {
            var row = new GameObject(objectName + "Row", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            var rt = (RectTransform)row.transform;
            rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(600, 60);
            rt.anchoredPosition = Vector2.zero;

            var labelText = CreateTmpText("Label", row.transform, label, 28);
            var labelRt = labelText.rectTransform;
            labelRt.anchorMin = new Vector2(0f, 0f); labelRt.anchorMax = new Vector2(0.35f, 1f);
            labelRt.offsetMin = Vector2.zero; labelRt.offsetMax = Vector2.zero;
            labelText.alignment = TextAlignmentOptions.MidlineLeft;

            var sliderGo = new GameObject(objectName, typeof(RectTransform), typeof(Slider));
            sliderGo.transform.SetParent(row.transform, false);
            var sRt = (RectTransform)sliderGo.transform;
            sRt.anchorMin = new Vector2(0.38f, 0.2f); sRt.anchorMax = new Vector2(1f, 0.8f);
            sRt.offsetMin = Vector2.zero; sRt.offsetMax = Vector2.zero;

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(sliderGo.transform, false);
            var bgRt = (RectTransform)bg.transform;
            bgRt.anchorMin = new Vector2(0, 0.4f); bgRt.anchorMax = new Vector2(1, 0.6f);
            bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
            bg.GetComponent<Image>().color = new Color(1, 1, 1, 0.15f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderGo.transform, false);
            var faRt = (RectTransform)fillArea.transform;
            faRt.anchorMin = new Vector2(0, 0.4f); faRt.anchorMax = new Vector2(1, 0.6f);
            faRt.offsetMin = Vector2.zero; faRt.offsetMax = Vector2.zero;

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            var fillRt = (RectTransform)fill.transform;
            fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero; fillRt.offsetMax = Vector2.zero;
            fill.GetComponent<Image>().color = new Color(0.9f, 0.9f, 0.95f, 0.9f);

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(sliderGo.transform, false);
            var haRt = (RectTransform)handleArea.transform;
            haRt.anchorMin = Vector2.zero; haRt.anchorMax = Vector2.one;
            haRt.offsetMin = Vector2.zero; haRt.offsetMax = Vector2.zero;

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            var hRt = (RectTransform)handle.transform;
            hRt.sizeDelta = new Vector2(20, 40);

            var slider = sliderGo.GetComponent<Slider>();
            slider.fillRect = fillRt;
            slider.handleRect = hRt;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f; slider.maxValue = 1f; slider.value = 1f;

            return slider;
        }

        private static Toggle CreateToggle(string objectName, Transform parent, string label, Vector2 anchor)
        {
            var row = new GameObject(objectName + "Row", typeof(RectTransform), typeof(Toggle));
            row.transform.SetParent(parent, false);
            var rt = (RectTransform)row.transform;
            rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(400, 60);
            rt.anchoredPosition = Vector2.zero;

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(row.transform, false);
            var bgRt = (RectTransform)bg.transform;
            bgRt.anchorMin = new Vector2(0f, 0.25f); bgRt.anchorMax = new Vector2(0.12f, 0.75f);
            bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
            bg.GetComponent<Image>().color = new Color(1, 1, 1, 0.15f);

            var checkmark = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            checkmark.transform.SetParent(bg.transform, false);
            var cmRt = (RectTransform)checkmark.transform;
            cmRt.anchorMin = Vector2.zero; cmRt.anchorMax = Vector2.one;
            cmRt.offsetMin = new Vector2(6, 6); cmRt.offsetMax = new Vector2(-6, -6);
            checkmark.GetComponent<Image>().color = Color.white;

            var labelText = CreateTmpText("Label", row.transform, label, 28);
            var labelRt = labelText.rectTransform;
            labelRt.anchorMin = new Vector2(0.15f, 0f); labelRt.anchorMax = new Vector2(1f, 1f);
            labelRt.offsetMin = Vector2.zero; labelRt.offsetMax = Vector2.zero;
            labelText.alignment = TextAlignmentOptions.MidlineLeft;

            var toggle = row.GetComponent<Toggle>();
            toggle.targetGraphic = bg.GetComponent<Image>();
            toggle.graphic = checkmark.GetComponent<Image>();
            toggle.isOn = true;

            return toggle;
        }

        private static GameObject InstantiateScreenPrefabOrFallback(Transform parent, string prefabPath, System.Func<Transform, GameObject> fallback)
        {
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabAsset != null)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset, parent);
                go.SetActive(false);
                return go;
            }

            Debug.LogWarning($"[TemplateSceneSetup] Missing prefab at {prefabPath}; using procedural fallback.");
            var built = fallback(parent);
            if (built != null) built.SetActive(false);
            return built;
        }

        private static void ConnectScreensInScene(string scenePath, bool wireEndGame)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            GameObject uiRoot = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root != null && root.name == "UIRoot")
                {
                    uiRoot = root;
                    break;
                }
            }

            if (uiRoot == null)
            {
                Debug.LogWarning($"[TemplateSceneSetup] UIRoot not found in {scenePath}.");
                return;
            }

            var parent = uiRoot.transform;

            if (wireEndGame)
            {
                ReplaceScreenWithPrefab(parent, "LeaderboardScreen", LeaderboardScreenPrefabPath, BuildLeaderboardUI);
                var leaderboardTr = parent.Find("LeaderboardScreen");
                if (leaderboardTr == null)
                {
                    Debug.LogWarning($"[TemplateSceneSetup] Could not place LeaderboardScreen under UIRoot in {scenePath}.");
                    EditorSceneManager.SaveScene(scene);
                    return;
                }

                leaderboardTr.gameObject.SetActive(false);
                var endGame = uiRoot.GetComponentInChildren<EndGameScreen>(true);
                if (endGame != null)
                {
                    var lb = leaderboardTr.GetComponent<LeaderboardScreen>();
                    if (lb != null)
                    {
                        SetField(endGame, "_leaderboardScreen", lb);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(endGame);
                    }
                }
            }
            else
            {
                ReplaceScreenWithPrefab(parent, "NicknameEntryScreen", NicknameEntryScreenPrefabPath, BuildNicknameEntryUI);
                ReplaceScreenWithPrefab(parent, "LeaderboardScreen", LeaderboardScreenPrefabPath, BuildLeaderboardUI);

                var nicknameTr = parent.Find("NicknameEntryScreen");
                var leaderboardTr = parent.Find("LeaderboardScreen");
                if (nicknameTr == null || leaderboardTr == null)
                {
                    Debug.LogWarning($"[TemplateSceneSetup] Could not place Nickname/Leaderboard under UIRoot in {scenePath}.");
                    EditorSceneManager.SaveScene(scene);
                    return;
                }

                nicknameTr.gameObject.SetActive(false);
                leaderboardTr.gameObject.SetActive(false);

                var mainMenu = uiRoot.GetComponentInChildren<MainMenuScreen>(true);
                if (mainMenu != null)
                {
                    var nick = nicknameTr.GetComponent<NicknameEntryScreen>();
                    var lb = leaderboardTr.GetComponent<LeaderboardScreen>();
                    if (nick != null) SetField(mainMenu, "_nicknameEntryScreen", nick);
                    if (lb != null) SetField(mainMenu, "_leaderboardScreen", lb);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(mainMenu);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ReplaceScreenWithPrefab(Transform parent, string childName, string prefabPath, System.Func<Transform, GameObject> fallback)
        {
            var existing = parent.Find(childName);
            if (existing != null)
                UnityEngine.Object.DestroyImmediate(existing.gameObject);

            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabAsset != null)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset, parent);
                go.name = childName;
                return;
            }

            var built = fallback(parent);
            if (built != null)
                built.name = childName;
        }

        private static void AddScenesToBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BootstrapScenePath, true),
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(GameScenePath, true),
            };
        }

        private static void SetField(UnityEngine.Object target, string fieldName, UnityEngine.Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[TemplateSceneSetup] Field {fieldName} not found on {target.GetType().Name}.");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetField(UnityEngine.Object target, string fieldName, bool value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[TemplateSceneSetup] Field {fieldName} not found on {target.GetType().Name}.");
                return;
            }
            prop.boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
