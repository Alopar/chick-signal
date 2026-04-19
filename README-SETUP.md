# Ludum Dare Template — Setup

Unity **6000.4.2f1**, URP **2D Renderer**, Input System, Cinemachine 3.

> **Русская документация:** `Assets/_Project/Documentation/` (начни с [`00_Старт.md`](Assets/_Project/Documentation/00_Старт.md)).
> Для будущих чатов с ИИ-ассистентами ссылайся на [`09_Для_LLM_агентов.md`](Assets/_Project/Documentation/09_Для_LLM_агентов.md).

## 1. First-time manual steps

After opening the project in Unity, do these once:

### TextMeshPro Essentials
`Window → TextMeshPro → Import TMP Essential Resources` (Examples optional).

### DOTween (optional, recommended for juicy UI/game tweens)
- Package Manager → `+` → **Install package from git URL** → `https://github.com/Demigiant/dotween.git`, **or**
- Download the `.unitypackage` from <https://dotween.demigiant.com/download.php> and import into `Assets/Plugins/Demigiant/`.
- Open `Tools → Demigiant → DOTween Utility Panel → Setup DOTween…`.

The template compiles and runs without DOTween; UI fades use plain coroutines.

### AudioMixer exposed parameters
`Tools/LudumDare/Generate Audio Mixer` already created `Assets/_Project/Audio/Mixers/MainMixer.mixer` with **Master**, **Music**, and **SFX** groups. You need to expose each group's volume:

1. Open `MainMixer.mixer` (Audio Mixer window).
2. Select the **Master** group → in the Inspector right-click the **Volume** value → **Expose 'Volume (of Master)' to script** → rename the exposed parameter to `Master`.
3. Repeat for **Music** → `Music` and **SFX** → `SFX`.

The exposed parameter names intentionally match the group names (`Master`, `Music`, `SFX`) to avoid confusion. Without this step the Settings sliders won't affect sound.

### Template regeneration (not needed, here for reference)
If you blow away the prefabs or scenes, run `Tools/LudumDare/Build Template (Prefabs + Scenes)` to rebuild them from scratch.

## 2. Entry points

Open `Assets/_Project/Scenes/00_Bootstrap.unity` and press Play.

The flow is:

```
00_Bootstrap  →  01_MainMenu  →  02_Game  (with EndGame as overlay)
```

Scenes are already in **Build Settings** in that order.

## 3. Project layout

```
Assets/_Project/
  Art/           Sprites · UI · Fonts · Animations · VFX
  Audio/         Music · SFX · Mixers/MainMixer.mixer
  Prefabs/       Managers/_Managers · UI/UIRoot · Gameplay/Player
  Scenes/        00_Bootstrap · 01_MainMenu · 02_Game
  ScriptableObjects/
    EventChannels/   (OnGameStateChanged, OnScoreChanged, OnGamePaused, OnGameResumed, OnLoadStart, OnLoadComplete)
    Configs/         (InputReader)
    AudioCues/       (empty — drop new AudioCue assets here)
  Scripts/
    Core/          Singleton · GameBootstrapper · ServiceLocator · ObjectPool
    Managers/      GameManager · SceneLoader · AudioManager · SaveManager · PauseService · AudioCueSO · GameState
    Events/        VoidEventChannelSO · IntEventChannelSO · FloatEventChannelSO · StringEventChannelSO · GameStateEventChannelSO
    UI/            UIScreen · UIManager · MainMenuScreen · SettingsScreen · PauseScreen · HUDScreen · EndGameScreen · LoadingScreen · PauseInputWatcher
    Input/         InputReader (SO) + InputSystem_Actions.inputactions
    Gameplay/      (empty — put game code here)
    Editor/Setup/  TemplateSetup · TemplateSceneSetup (menu tools)
  Settings/      GlobalVolumeProfile (PostProcessing) · Lit2DSceneTemplate
```

## 4. Architecture cheat-sheet

- **Singletons** (`LudumDare.Template.Core.Singleton<T>`) for managers — created once in `_Managers.prefab` and `DontDestroyOnLoad`-ed by `GameBootstrapper`.
- **ScriptableObject event channels** for decoupled UI ↔ gameplay wiring. Drop the same `*EventChannelSO` asset into both publisher and subscriber.
- **UI stack** (`UIManager.Push/Pop/Replace`) — every screen derives from `UIScreen` and fades via `CanvasGroup`.

### Typical flows

- Start game: `SceneLoader.Instance.LoadGame()`
- Pause: `PauseInputWatcher` catches ESC → `PauseService.Pause()` → shows PauseScreen.
- Score up: `GameManager.Instance.AddScore(10)` → publishes `OnScoreChanged` → HUD refreshes.
- Game over: `GameManager.Instance.GameOver()` → state channel fires → `EndGameScreen` shows overlay.

### Adding a new UI screen

1. Create a class deriving from `UIScreen`, hook buttons in `Awake()`.
2. Build the prefab under `Assets/_Project/Prefabs/UI/Screens/`.
3. Reference it where needed (`[SerializeField] private MyScreen _myScreen;`) and call `UIManager.Instance.Push(_myScreen)`.

### Adding a new event

1. Create SO asset: `Assets/.../ScriptableObjects/EventChannels/` → `Create → LudumDare → Events → <Type> Event Channel`.
2. Serialize-reference the asset from publisher + subscribers.
3. Publisher calls `channel.Raise(value)`; subscribers subscribe/unsubscribe to `channel.OnEventRaised` in `OnEnable`/`OnDisable`.

## 5. Packages

Managed via `Packages/manifest.json`. Highlights:

- `com.unity.cinemachine` 3.1.2
- `com.unity.render-pipelines.universal` 17.4.0 (2D Renderer)
- `com.unity.inputsystem` 1.19.0
- `com.unity.ugui` 2.0.0 (bundles TMP)
- Full 2D toolset (Animation, Aseprite, PSD, SpriteShape, Tilemap)

Removed to keep the project lean: Visual Scripting, Multiplayer Center.

## 6. Tags / Layers / Sorting Layers

- Tags: `Player`, `Enemy`, `Pickup`, `Interactable`, `Projectile`.
- Layers: `Player`, `Enemy`, `Projectile`, `Interactable`, `Ground` (+ default).
- Sorting Layers: `Background → Midground → Entities → Foreground → UI`.
