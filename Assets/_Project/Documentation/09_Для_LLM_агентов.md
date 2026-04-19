# 09. Памятка для LLM-агентов

Файл-«источник истины» для будущих чатов с ИИ-ассистентами. Сошлись на него, и LLM получит нужный контекст без долгих поисков.

## Движок и пакеты

- Unity **6000.4.2f1**, C# / .NET как в Unity 6.
- URP **17.4.0** (2D Renderer).
- Input System **1.19.0**.
- Cinemachine **3.1.2**.
- uGUI + TextMeshPro.
- DOTween — опционально.

## Неймспейсы

- `LudumDare.Template.Core`
- `LudumDare.Template.Managers`
- `LudumDare.Template.Events`
- `LudumDare.Template.UI`
- `LudumDare.Template.Gameplay` (твой код)
- `LudumDare.Template.EditorTools` (`#if UNITY_EDITOR`)

## Ключевые инварианты

1. `_Managers.prefab` инстанцируется только в `00_Bootstrap` через `GameBootstrapper`. Все менеджеры — `Singleton<T>` из `Core/Singleton.cs`.
2. UI-навигация — через `UIManager.Push/Pop/Replace`. Все экраны наследуют `UIScreen`.
3. Стартовый экран сцены показывается автоматически: в инспекторе выставлены `_showOnStart = true` и `_replacesStack = true`.
4. Громкость — только через `AudioManager.Set*Volume`. Имена exposed-параметров микшера: **`Master`**, **`Music`**, **`SFX`** (совпадают с именами групп).
5. Пауза — только через `PauseService`. Не трогать `Time.timeScale` напрямую.
6. Развязанные события — через ScriptableObject EventChannels в `ScriptableObjects/EventChannels/`.
7. Сохранения — через `SaveManager`, обёртку над `PlayerPrefs`.
8. В `Assets/` на верхнем уровне — только папки. Отдельные ассеты — в подпапках (например, `Assets/Settings/`).

## Как добавлять фичи (быстрая шпаргалка)

| Задача | Куда смотреть |
|---|---|
| Новый UI-экран | [`02_UI_экраны.md`](02_UI_экраны.md) |
| Новое событие геймплея | [`03_События_EventChannels.md`](03_События_EventChannels.md) |
| Новый звук / музыка | [`04_Звук_и_AudioCue.md`](04_Звук_и_AudioCue.md) |
| Новое сохраняемое поле | [`05_Сохранения_и_настройки.md`](05_Сохранения_и_настройки.md) |
| Новый менеджер, пул, счёт, загрузка сцены | [`07_Геймплей_рецепты.md`](07_Геймплей_рецепты.md) |

## Стиль кода

- Приватные поля — с подчёркиванием и camelCase: `_playerHealth`.
- Сериализуемые поля — `[SerializeField] private T _name;` (не public-поля).
- Комментарии `///` для публичных классов и сложных методов; обычные — только если поясняют непонятное «зачем», а не «что».
- Обработчики событий: `OnXxx` или `HandleXxx`. Всегда отписываться в `OnDisable`/`OnDestroy`.
- Корутины UI и пауз-независимая логика — на `Time.unscaledDeltaTime`.
- Публичный API менеджера — минимальный, всё остальное приватно.

## Что категорически не надо делать

- **Не создавать второй `_Managers.prefab`** и не класть его в сцены вручную.
- **Не переименовывать exposed-параметры микшера** (`Master`/`Music`/`SFX`) — сломаются сеттеры громкости.
- **Не писать в `Time.timeScale` напрямую** из геймплея — используй `PauseService`.
- **Не ссылаться на экраны из других сцен** — они уничтожаются при смене сцены. Для кроссценовой коммуникации используй EventChannels.
- **Не делать `FindObjectOfType<Singleton>()`** каждый кадр — бери через `Singleton.Instance` после `HasInstance`.

## Где быстро посмотреть

- Точка входа — [`GameBootstrapper.cs`](../Scripts/Core/GameBootstrapper.cs).
- Сцены — `Assets/_Project/Scenes/`.
- Менеджеры — `Assets/_Project/Scripts/Managers/`.
- UI-экраны — `Assets/_Project/Scripts/UI/`.
- Каналы событий — `Assets/_Project/Scripts/Events/` + ассеты в `ScriptableObjects/EventChannels/`.
- Регенерация шаблона — `Assets/_Project/Scripts/Editor/Setup/TemplateSceneSetup.cs` (меню `Tools/LudumDare/Build Template (Prefabs + Scenes)`).
