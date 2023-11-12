using UnityEditorInternal;
using UnityEngine;

namespace HybridCLR.Editor.Settings
{
    [FilePath("ProjectSettings/HybridCLRSettings.asset")]
    public class HybridCLRSettings : ScriptableSingleton<HybridCLRSettings>
    {
        [Header("开启HybridCLR插件")]
        public bool enable = true;

        [Header("使用全局安装的il2cpp")]
        public bool useGlobalIl2cpp;

        [Header("hybridclr 仓库 URL")]
        public string hybridclrRepoURL = "https://gitee.com/focus-creative-games/hybridclr";

        [Header("il2cpp_plus 仓库 URL")]
        public string il2cppPlusRepoURL = "https://gitee.com/focus-creative-games/il2cpp_plus";

        [Header("热更新Assembly Definitions")]
        public AssemblyDefinitionAsset[] hotUpdateAssemblyDefinitions;

        [Header("热更新dlls")]
        public string[] hotUpdateAssemblies;

        [Header("预留的热更新dlls")]
        public string[] preserveHotUpdateAssemblies;

        [Header("热更新dll编译输出根目录")]
        public string hotUpdateDllCompileOutputRootDir = "HybridCLRData/HotUpdateDlls";

        [Header("外部热更新dll搜索路径")]
        public string[] externalHotUpdateAssembliyDirs;

        [Header("裁减后AOT dll输出根目录")]
        public string strippedAOTDllOutputRootDir = "HybridCLRData/AssembliesPostIl2CppStrip";

        [Header("补充元数据AOT dlls")]
        public string[] patchAOTAssemblies;

        [Header("生成的link.xml路径")]
        public string outputLinkFile = "HybridCLRGenerate/link.xml";

        [Header("自动扫描生成的AOTGenericReferences.cs路径")]
        public string outputAOTGenericReferenceFile = "HybridCLRGenerate/AOTGenericReferences.cs";

        [Header("AOT泛型实例化搜索迭代次数")]
        public int maxGenericReferenceIteration = 10;

        [Header("MethodBridge泛型搜索迭代次数")]
        public int maxMethodBridgeGenericIteration = 10;
        
        [Header("AOT dlls 复制目标目录")]
        public string copyAOTDllsTargetDir = "Assets/StreamingAssets/AOTDlls";

        [Header("复制AOT dlls时对AOTGenericReferences.PatchedAOTAssemblyList的补充")]
        public string[] copyAOTDllsAddition ={"System"};
        
        [Header("热更 dlls 复制目标目录")]
        public string copyHotUpdateDllsTargetDir = "Assets/StreamingAssets/HotUpdateDlls";

        [Header("复制热更dll时，忽略平台文件夹")]
        public bool ignorePlatformWhenCopyHotUpdateDll = true;
        
        [Header("复制Dlls附加后缀名")] 
        public string copyDllsExtension = ".bytes";
    }
}
