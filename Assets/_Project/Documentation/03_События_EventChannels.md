# 03. События (EventChannels)

Паттерн **ScriptableObject Event Channel** — способ развязать издателя и подписчика. Оба ссылаются на один `.asset` и не знают друг о друге.

## Типы каналов

В `Assets/_Project/Scripts/Events/`:

- `VoidEventChannelSO` — событие без аргумента.
- `IntEventChannelSO`, `FloatEventChannelSO`, `StringEventChannelSO` — с одним значением.
- `GameStateEventChannelSO` — передаёт enum `GameState`.

Каждый SO содержит `UnityAction<T> OnEventRaised` и метод `Raise(value)`.

## Готовые ассеты (в `ScriptableObjects/EventChannels/`)

| Ассет | Тип | Что значит |
|---|---|---|
| `OnGameStateChanged` | `GameStateEventChannelSO` | Смена состояния (`GameManager.SetState`). |
| `OnScoreChanged` | `IntEventChannelSO` | Новое значение счёта. |
| `OnGamePaused` | `VoidEventChannelSO` | Игра поставлена на паузу. |
| `OnGameResumed` | `VoidEventChannelSO` | Игра снята с паузы. |
| `OnLoadStart` | `VoidEventChannelSO` | Началась загрузка сцены. |
| `OnLoadComplete` | `VoidEventChannelSO` | Закончилась загрузка сцены. |

## Как создать свой канал

1. Если подходит один из существующих типов — просто создай новый ассет: `Assets/_Project/ScriptableObjects/EventChannels/ → ПКМ → Create → LudumDare → Events → <Type> Event Channel`.
2. Если нужен свой тип данных — добавь файл `MyEventChannelSO.cs` в `Assets/_Project/Scripts/Events/` по образцу `IntEventChannelSO`.

## Как использовать

**Издатель:**

1. Серилиазуй поле `[SerializeField] private IntEventChannelSO _scoreChannel;`.
2. В инспекторе перетащи ассет канала.
3. Вызывай `_scoreChannel?.Raise(value)`.

**Подписчик:**

1. То же поле в инспекторе.
2. В `OnEnable` — `_scoreChannel.OnEventRaised += Handler;`.
3. В `OnDisable` — `_scoreChannel.OnEventRaised -= Handler;` (обязательно отписываемся).

## Рекомендации

- Один канал — одно осмысленное событие. Не делай «универсальные» каналы.
- Канал не хранит состояние. Если подписчик появился позже — события из прошлого он не увидит. Для «последнее значение» — делай свойство в синглтоне и читай напрямую.
- Всегда отписывайся в `OnDisable`/`OnDestroy`, иначе получишь NullReferenceException после перезагрузки сцены.
