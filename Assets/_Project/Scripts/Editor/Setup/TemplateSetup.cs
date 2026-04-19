#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LudumDare.Template.EditorTools
{
    /// <summary>
    /// One-shot setup helpers. Run from the <c>Tools/LudumDare</c> menu after opening the template for
    /// the first time. Safe to re-run: it skips assets that already exist.
    /// </summary>
    public static class TemplateSetup
    {
        private const string MixerPath = "Assets/_Project/Audio/Mixers/MainMixer.mixer";
        private const string VolumeProfilePath = "Assets/_Project/Settings/PostProcessing/GlobalVolumeProfile.asset";

        [MenuItem("Tools/LudumDare/Generate Audio Mixer")]
        public static void GenerateAudioMixer()
        {
            if (File.Exists(MixerPath))
            {
                Debug.Log($"[TemplateSetup] Mixer already exists at {MixerPath}. Skipped.");
                return;
            }

            var controllerType = FindType("UnityEditor.Audio.AudioMixerController");
            var groupType      = FindType("UnityEditor.Audio.AudioMixerGroupController");
            if (controllerType == null || groupType == null)
            {
                Debug.LogError("[TemplateSetup] Internal AudioMixer types not found. Create the mixer manually and expose Master/Music/SFX volumes.");
                return;
            }

            var create = controllerType.GetMethod("CreateMixerControllerAtPath",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

            if (create == null)
            {
                Debug.LogWarning("[TemplateSetup] CreateMixerControllerAtPath not available in this Unity version. Create the mixer manually.");
                return;
            }

            var controller = create.Invoke(null, new object[] { MixerPath });
            if (controller == null)
            {
                Debug.LogError("[TemplateSetup] Failed to create mixer.");
                return;
            }

            var masterGroupProp = controllerType.GetProperty("masterGroup");
            var master = masterGroupProp?.GetValue(controller);

            var createSubGroup = controllerType.GetMethod("CreateNewSubGroup", new[] { typeof(string), groupType });
            if (createSubGroup != null && master != null)
            {
                createSubGroup.Invoke(controller, new object[] { "Music", master });
                createSubGroup.Invoke(controller, new object[] { "SFX", master });
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(MixerPath);
            Debug.Log($"[TemplateSetup] Created AudioMixer at {MixerPath}. Expose each group's Volume manually (right-click Volume → Expose) and rename exposed params to Master / Music / SFX.");
        }

        private static System.Type FindType(string fullName)
        {
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(fullName);
                if (t != null) return t;
            }
            return null;
        }

        [MenuItem("Tools/LudumDare/Generate Global Volume Profile")]
        public static void GenerateVolumeProfile()
        {
            if (File.Exists(VolumeProfilePath))
            {
                Debug.Log($"[TemplateSetup] Volume profile already exists at {VolumeProfilePath}. Skipped.");
                return;
            }

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, VolumeProfilePath);

            var bloom = profile.Add<UnityEngine.Rendering.Universal.Bloom>(true);
            bloom.active = false;
            bloom.intensity.value = 0.4f;
            bloom.threshold.value = 0.9f;

            var vignette = profile.Add<UnityEngine.Rendering.Universal.Vignette>(true);
            vignette.active = false;
            vignette.intensity.value = 0.25f;

            var chromatic = profile.Add<UnityEngine.Rendering.Universal.ChromaticAberration>(true);
            chromatic.active = false;
            chromatic.intensity.value = 0.2f;

            AssetDatabase.SaveAssets();
            Debug.Log($"[TemplateSetup] Created Global Volume Profile at {VolumeProfilePath} (all effects disabled by default).");
        }
    }
}
#endif
