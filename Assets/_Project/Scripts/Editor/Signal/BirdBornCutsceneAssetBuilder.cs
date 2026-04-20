#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using LudumDare.Template.Gameplay.Signal;

namespace LudumDare.Template.Editor.Signal
{
    /// <summary>
    /// Генерирует ассеты вводной кат-сцены BirdBorn из листа спрайтов.
    /// </summary>
    public static class BirdBornCutsceneAssetBuilder
    {
        public const string TexturePath = "Assets/_Project/Art/Sprites/BirdBorn/birdBornAnimation.png";
        public const string ClipAssetPath = "Assets/_Project/Art/Sprites/BirdBorn/BirdBornCutscene.anim";
        public const string ControllerAssetPath = "Assets/_Project/Art/Sprites/BirdBorn/BirdBornCutscene.controller";
        public const string PrefabPath = "Assets/_Project/Prefabs/Gameplay/BirdBornCutscene.prefab";
        public const string IntroFlowPrefabPath = "Assets/_Project/Prefabs/Gameplay/GameIntroFlow.prefab";

        private const float FramesPerSecond = 12f;

        [MenuItem("Tools/Signal/Setup Bird Born Cutscene (рекомендуется: префаб со спрайтами)")]
        public static void MenuSetupSpritePrefab()
        {
            CreatePrefabSpritePlayerOnly();
        }

        [MenuItem("Tools/Signal/Create Game Intro Flow Prefab")]
        public static void MenuCreateIntroFlowPrefab()
        {
            CreateIntroFlowPrefab();
        }

        [MenuItem("Tools/Signal/Create Game Intro Flow Prefab (контроллер + кат-сцена)")]
        public static void MenuCreateIntroFlowPrefabRu()
        {
            CreateIntroFlowPrefab();
        }

        [MenuItem("Tools/Signal/Generate Bird Born Cutscene (anim + controller)")]
        public static void GenerateAnimAndController()
        {
            var sprites = LoadSpritesOrdered(TexturePath);
            if (sprites.Count == 0)
            {
                Debug.LogError($"[BirdBornCutscene] Не найдены спрайты в {TexturePath}");
                return;
            }

            float frameDt = 1f / FramesPerSecond;
            var keyframes = new ObjectReferenceKeyframe[sprites.Count];
            for (int i = 0; i < sprites.Count; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe { time = i * frameDt, value = sprites[i] };
            }

            var binding = new EditorCurveBinding
            {
                path = "",
                type = typeof(SpriteRenderer),
                propertyName = "m_Sprite"
            };

            var clip = new AnimationClip { name = "BirdBornCutscene", frameRate = FramesPerSecond };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            if (AssetDatabase.LoadAssetAtPath<Object>(ClipAssetPath) != null)
                AssetDatabase.DeleteAsset(ClipAssetPath);
            AssetDatabase.CreateAsset(clip, ClipAssetPath);

            if (AssetDatabase.LoadAssetAtPath<Object>(ControllerAssetPath) != null)
                AssetDatabase.DeleteAsset(ControllerAssetPath);

            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerAssetPath);
            AnimatorStateMachine root = controller.layers[0].stateMachine;
            AnimatorState state = root.AddState("BirdBorn");
            state.motion = clip;
            root.defaultState = state;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log($"[BirdBornCutscene] Готово: {sprites.Count} кадров, ~{sprites.Count * frameDt:F2} с → {ClipAssetPath}");
        }

        [MenuItem("Tools/Signal/Create Bird Born Cutscene Prefab (только Animator)")]
        public static void CreatePrefabAnimatorOnly()
        {
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerAssetPath);
            if (controller == null)
            {
                Debug.LogError($"[BirdBornCutscene] Сначала сгенерируйте контроллер: {ControllerAssetPath} отсутствует.");
                return;
            }

            var go = new GameObject("BirdBornCutscene");
            var sr = go.AddComponent<SpriteRenderer>();
            var anim = go.AddComponent<Animator>();
            anim.runtimeAnimatorController = controller;

            EnsurePrefabDirectory();
            if (AssetDatabase.LoadAssetAtPath<Object>(PrefabPath) != null)
                AssetDatabase.DeleteAsset(PrefabPath);

            PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            Object.DestroyImmediate(go);
            Debug.Log($"[BirdBornCutscene] Префаб (Animator): {PrefabPath}");
        }

        /// <summary>Создаёт префаб с <see cref="BirdBornCutsceneSpritePlayer"/> и заполненным массивом кадров (без Animator).</summary>
        public static void CreatePrefabSpritePlayerOnly()
        {
            var sprites = LoadSpritesOrdered(TexturePath);
            if (sprites.Count == 0)
            {
                Debug.LogError($"[BirdBornCutscene] Нет спрайтов в {TexturePath}. Проверьте импорт Multiple / Slice.");
                return;
            }

            var go = new GameObject("BirdBornCutscene");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprites[0];
            sr.sortingOrder = 500;
            var player = go.AddComponent<BirdBornCutsceneSpritePlayer>();
            var so = new SerializedObject(player);
            so.FindProperty("_frames").arraySize = sprites.Count;
            for (int i = 0; i < sprites.Count; i++)
                so.FindProperty("_frames").GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
            so.FindProperty("_framesPerSecond").floatValue = FramesPerSecond;
            so.FindProperty("_spriteRenderer").objectReferenceValue = sr;
            so.FindProperty("_playOnEnable").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();

            EnsurePrefabDirectory();
            if (AssetDatabase.LoadAssetAtPath<Object>(PrefabPath) != null)
                AssetDatabase.DeleteAsset(PrefabPath);

            PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            Object.DestroyImmediate(go);
            AssetDatabase.SaveAssets();
            Debug.Log($"[BirdBornCutscene] Префаб со спрайтами: {PrefabPath} ({sprites.Count} кадров). Перетащите в сцену или используйте {IntroFlowPrefabPath}.");
        }

        /// <summary>Корень с <see cref="SignalIntroCutsceneController"/> и дочерним BirdBorn — один объект в иерархию сцены.</summary>
        public static void CreateIntroFlowPrefab()
        {
            CreatePrefabSpritePlayerOnly();
            var birdBornPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (birdBornPrefab == null)
            {
                Debug.LogError("[BirdBornCutscene] Нет префаба BirdBornCutscene.");
                return;
            }

            var root = new GameObject("GameIntroFlow");
            var intro = root.AddComponent<SignalIntroCutsceneController>();
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(birdBornPrefab, root.transform);
            instance.transform.localPosition = Vector3.zero;
            instance.name = "BirdBornCutscene";

            var spritePlayer = instance.GetComponent<BirdBornCutsceneSpritePlayer>();
            var so = new SerializedObject(intro);
            so.FindProperty("_spriteCutscene").objectReferenceValue = spritePlayer;
            so.FindProperty("_cutsceneVisualRoot").objectReferenceValue = instance;
            so.ApplyModifiedPropertiesWithoutUndo();

            EnsurePrefabDirectory();
            if (AssetDatabase.LoadAssetAtPath<Object>(IntroFlowPrefabPath) != null)
                AssetDatabase.DeleteAsset(IntroFlowPrefabPath);

            PrefabUtility.SaveAsPrefabAsset(root, IntroFlowPrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log($"[BirdBornCutscene] Готовый поток интро: {IntroFlowPrefabPath} — перетащите в сцену 02_Game.");
        }

        private static void EnsurePrefabDirectory()
        {
            string dir = Path.GetDirectoryName(PrefabPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        private static List<Sprite> LoadSpritesOrdered(string assetPath)
        {
            var all = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            var list = new List<Sprite>();
            foreach (var o in all)
            {
                if (o is Sprite s)
                    list.Add(s);
            }

            return list.OrderBy(s =>
            {
                var m = Regex.Match(s.name, @"_(\d+)$");
                return m.Success ? int.Parse(m.Groups[1].Value) : 0;
            }).ToList();
        }
    }

    [InitializeOnLoad]
    internal static class BirdBornCutscenePrefabAutoCreate
    {
        static BirdBornCutscenePrefabAutoCreate()
        {
            EditorApplication.delayCall += TryCreatePrefabIfMissing;
        }

        private static void TryCreatePrefabIfMissing()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (AssetDatabase.LoadAssetAtPath<GameObject>(BirdBornCutsceneAssetBuilder.IntroFlowPrefabPath) != null)
                return;

            if (!File.Exists(BirdBornCutsceneAssetBuilder.TexturePath.Replace('/', Path.DirectorySeparatorChar)))
                return;

            BirdBornCutsceneAssetBuilder.CreatePrefabSpritePlayerOnly();
            BirdBornCutsceneAssetBuilder.CreateIntroFlowPrefab();
        }
    }
}
#endif
