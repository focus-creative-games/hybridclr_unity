using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
static class HybridCLRGlobalSettingsLoader
{
    static string path = "Assets/HybridCLR Data";
    static string file = $"{path}/HybridCLRGlobalSettings.asset";
    
    [MenuItem("Assets/Create/HybridCLR", menuItem = "Assets/Create/HybridCLR/GlobalSettings", priority =90)]
    static void CreateOrLoadHybridCLRSettings()
    {
        var settings = AssetDatabase.LoadAssetAtPath<HybridCLRGlobalSettings>(file);
        if (!settings)
        {
            settings = ScriptableObject.CreateInstance<HybridCLRGlobalSettings>();
            var fullPath = Path.GetFullPath(path);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }
            AssetDatabase.CreateAsset(settings, file);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        EditorGUIUtility.PingObject(settings);
    }
    class SingletonValidate : AssetPostprocessor
    {
        public static void OnPostprocessAllAssets(string[] importedAsset, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
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
}

public class HybridCLRGlobalSettings : ScriptableObject
{
    [Header("开启HybridCLR插件")]
    public bool enable = true;

    [Header("从gitee clone插件代码")]
    public bool cloneFromGitee = true; // false 则从github上拉取

    [Header("热更新Assembly Definition Modules")]
    public AssemblyDefinitionAsset[] hotUpdateAssemblyDefinitions;

    [Header("热更新dlls")]
    public string[] hotUpdateAssemblies;

    [Header("自动扫描生成的link.xml路径")]
    public string outputLinkFile = "HybridCLR/link.xml";

    [Header("自动扫描生成的AOTGenericReferences.cs路径")]
    public string outputAOTGenericReferenceFile = "Main/AOTGenericReferences.cs";

    [Header("AOT泛型实例化搜索迭代次数")]
    public int maxGenericReferenceIteration = 4;

    [Header("预留MonoPInvokeCallbackAttribute函数个数")]
    public int ReversePInvokeWrapperCount = 10;

    [Header("MethodBridge泛型搜索迭代次数")]
    public int maxMethodBridgeGenericIteration = 4;

    [Header("热更新dll输出目录（相对HybridCLRData目录）")]
    public string hotUpdateDllOutputDir = "HotUpdateDlls";

    [Header("HybridCLRData目录（相对工程目录）")]
    public string hybridCLRDataDir = "HybridCLRData";

    [Header("裁剪后的AOT assembly输出目录")]
    public string strippedAssemblyDir = "AssembliesPostIl2CppStrip";
}
