# 04. Звук и AudioCue

## AudioMixer

Файл: `Assets/_Project/Audio/Mixers/MainMixer.mixer`.

Группы:
- `Master` — корневая группа.
- `Music` — дочерняя, для фоновой музыки.
- `SFX` — дочерняя, для звуковых эффектов.

Exposed-параметры (именно их читает/пишет код):
- **`MasterVolume`**
- **`MusicVolume`**
- **`SFXVolume`**

Если по какой-то причине exposed-параметров нет, см. [`00_Старт.md`](00_Старт.md), раздел «AudioMixer».

## AudioManager

[`Assets/_Project/Scripts/Managers/AudioManager.cs`](../Scripts/Managers/AudioManager.cs) — живёт на `_Managers.prefab`. Что умеет:

- Держит пул `AudioSource` для SFX (`_sfxPoolSize` в инспекторе).
- Два источника для музыки с кроссфейдом (`_musicA`/`_musicB`).
- `SetMasterVolume(float)`, `SetMusicVolume(float)`, `SetSfxVolume(float)` — линейный `[0..1]`, внутри пересчитывается в dB. Дублируется в `SaveManager`.
- `ApplyVolumes(master, music, sfx)` — разом.
- `PlaySFX(AudioCueSO cue)`.
- `PlayMusic(AudioCueSO cue, float fadeSeconds = 1f)`.
- `StopMusic(float fadeSeconds = 1f)`.

На старте `AudioManager` сам прочитает сохранённые громкости из `SaveManager` и применит их в микшер.

## AudioCueSO

[`Assets/_Project/Scripts/Managers/AudioCueSO.cs`](../Scripts/Managers/AudioCueSO.cs) — ScriptableObject, «рецепт» звука:

- `Clips[]` — массив клипов (если несколько — выбирается случайно).
- `Volume [0..1]`.
- `PitchRange (Vector2)` — диапазон случайного pitch.
- `Loop`.
- `MixerGroup` — если не указан, используется пул `_sfxGroup`/`_musicGroup` из `AudioManager`.

## Добавить новый звук (рецепт)

1. Кинь аудио-файл в `Assets/_Project/Audio/SFX/` или `Assets/_Project/Audio/Music/`.
2. `Assets/_Project/ScriptableObjects/AudioCues/` → ПКМ → `Create → LudumDare → Audio → Audio Cue`.
3. В инспекторе залей `Clips`, настрой `Volume`/`PitchRange`.
4. Серилиазуй ссылку в компоненте: `[SerializeField] private AudioCueSO _hitCue;`.
5. Играй: `AudioManager.Instance.PlaySFX(_hitCue);` или `PlayMusic(_bgmCue)`.

## Настройка громкости игроком

`SettingsScreen` ([`SettingsScreen.cs`](../Scripts/UI/SettingsScreen.cs)) имеет три Slider-а. При изменении:

- Читает актуальные значения из `SaveManager`.
- На событие `onValueChanged` зовёт `AudioManager.Instance.SetMasterVolume(v)` и т.п.

Если звук не двигается — 99% случаев это неправильные exposed-имена в микшере.
