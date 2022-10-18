using UnityEditorInternal;
using UnityEngine;
namespace HybridCLR.Editor
{
    [FilePath("ProjectSettings/HybridCLRSettings.asset")]
    public class HybridCLRSettings : ScriptableSingleton<HybridCLRSettings>
    {
        [Header("开启HybridCLR插件")]
        public bool enable = true;

        [Header("使用全局安装的il2cpp")]
        public bool useGlobalIl2cpp;

        [Header("从gitee clone插件代码")]
        public bool cloneFromGitee = true; // false 则从github上拉取

        [Header("热更新Assembly Definitions")]
        public AssemblyDefinitionAsset[] hotUpdateAssemblyDefinitions;

        [Header("热更新dlls")]
        public string[] hotUpdateAssemblies;

        [Header("预留的热更新dlls")]
        public string[] preserveHotUpdateAssemblies;

        [Header("生成link.xml时扫描asset中引用的类型")]
        public bool collectAssetReferenceTypes;

        [Header("生成的link.xml路径")]
        public string outputLinkFile = "HybridCLRData/Generated/link.xml";

        [Header("自动扫描生成的AOTGenericReferences.cs路径")]
        public string outputAOTGenericReferenceFile = "HybridCLRData/Generated/AOTGenericReferences.cs";

        [Header("AOT泛型实例化搜索迭代次数")]
        public int maxGenericReferenceIteration = 10;

        //[Header("预留MonoPInvokeCallbackAttribute函数个数")]
        //public int ReversePInvokeWrapperCount = 10;

        [Header("MethodBridge泛型搜索迭代次数")]
        public int maxMethodBridgeGenericIteration = 10;
    }
}
