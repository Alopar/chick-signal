using LudumDare.Template.Gameplay.Signal;
using UnityEditor;
using UnityEngine;

namespace LudumDare.Template.Editor.Signal
{
    public static class SignalBalanceAssetMenu
    {
        private const string AssetPath = "Assets/_Project/ScriptableObjects/Configs/SignalGameBalance.asset";

        [MenuItem("LudumDare/Signal/Create Default Balance Asset")]
        public static void CreateDefaultBalance()
        {
            var asset = ScriptableObject.CreateInstance<SignalGameBalanceSO>();
            SignalBalanceDefaults.ApplyHtmlDefaults(asset);
            AssetDatabase.CreateAsset(asset, AssetPath);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(asset);
            Debug.Log($"[Signal] Created {AssetPath}");
        }

        [MenuItem("LudumDare/Signal/Apply HTML Defaults To Existing Balance")]
        public static void ApplyDefaultsToExisting()
        {
            var asset = AssetDatabase.LoadAssetAtPath<SignalGameBalanceSO>(AssetPath);
            if (asset == null)
            {
                Debug.LogError($"[Signal] No asset at {AssetPath}. Use Create Default Balance Asset first.");
                return;
            }

            Undo.RecordObject(asset, "Apply SIGNAL HTML balance defaults");
            SignalBalanceDefaults.ApplyHtmlDefaults(asset);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(asset);
            Debug.Log($"[Signal] Applied HTML defaults to {AssetPath}");
        }
    }
}
