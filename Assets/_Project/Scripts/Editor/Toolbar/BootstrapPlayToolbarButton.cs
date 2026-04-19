#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;

namespace LudumDare.Template.EditorTools
{
    [InitializeOnLoad]
    public static class BootstrapPlayToolbarButton
    {
        private const string BootstrapScenePath = "Assets/_Project/Scenes/00_Bootstrap.unity";
        private const string ToolbarElementId = "LudumDare.PlayFromBootstrap";

        private static SceneAsset _previousStartScene;
        private static bool _restoreStartSceneAfterPlay;

        static BootstrapPlayToolbarButton()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MenuItem("Tools/LudumDare/Play From Bootstrap %#&p")]
        public static void PlayFromBootstrapMenu()
        {
            PlayFromBootstrap();
        }

        [MainToolbarElement(
            ToolbarElementId,
            defaultDockPosition = MainToolbarDockPosition.Middle,
            defaultDockIndex = 0)]
        private static MainToolbarElement CreatePlayFromBootstrapToolbarButton()
        {
            return new MainToolbarButton(
                new MainToolbarContent("🔵", "Play from 00_Bootstrap scene"),
                PlayFromBootstrap);
        }

        private static void PlayFromBootstrap()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (!File.Exists(BootstrapScenePath))
            {
                EditorUtility.DisplayDialog(
                    "Bootstrap scene not found",
                    $"Scene was not found at:\n{BootstrapScenePath}",
                    "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var bootstrapScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapScenePath);
            if (bootstrapScene == null)
            {
                EditorUtility.DisplayDialog(
                    "Bootstrap scene not found",
                    $"Could not load scene at:\n{BootstrapScenePath}",
                    "OK");
                return;
            }

            _previousStartScene = EditorSceneManager.playModeStartScene;
            EditorSceneManager.playModeStartScene = bootstrapScene;
            _restoreStartSceneAfterPlay = true;
            EditorApplication.isPlaying = true;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!_restoreStartSceneAfterPlay || state != PlayModeStateChange.EnteredEditMode)
            {
                return;
            }

            EditorSceneManager.playModeStartScene = _previousStartScene;
            _previousStartScene = null;
            _restoreStartSceneAfterPlay = false;
        }
    }
}
#endif
