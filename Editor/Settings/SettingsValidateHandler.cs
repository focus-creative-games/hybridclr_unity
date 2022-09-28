using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 用于监控 Global Settings 单例状态
/// </summary>
class SettingsValidateHandler : AssetPostprocessor
{
    public static void OnPostprocessAllAssets(string[] importedAsset, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        var file = $"Assets/HybridCLRData/{nameof(HybridCLRGlobalSettings)}.asset";
        var duplicate = importedAsset.Where(v => v.EndsWith(".asset"))
            .Select(v => new { key = v, asset = AssetDatabase.LoadAssetAtPath<HybridCLRGlobalSettings>(v) })
            .Where(v => v.asset && v.key != file)
            .Select(v => v.key)
            .ToArray();
        if (duplicate.Length > 0)
        {
            Debug.LogError($"HybridCLRGlobalSettings 配置冗余：\n{string.Join("\n", duplicate)}");
        }
    }
}