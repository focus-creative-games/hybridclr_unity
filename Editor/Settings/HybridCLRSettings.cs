using UnityEditorInternal;
using UnityEngine;

namespace HybridCLR.Editor.Settings
{
    [FilePath("ProjectSettings/HybridCLRSettings.asset")]
    public class HybridCLRSettings : ScriptableSingleton<HybridCLRSettings>
    {
        [Tooltip("enable HybridCLR")]
        public bool enable = true;

        [Tooltip("use il2cpp in unity editor installation location")]
        public bool useGlobalIl2cpp;

        [Tooltip("hybridclr repo URL")]
        public string hybridclrRepoURL = "https://gitee.com/focus-creative-games/hybridclr";

        [Tooltip("il2cpp_plus repo URL")]
        public string il2cppPlusRepoURL = "https://gitee.com/focus-creative-games/il2cpp_plus";

        [Tooltip("hot update assembly definitions(asd)")]
        public AssemblyDefinitionAsset[] hotUpdateAssemblyDefinitions;

        [Tooltip("hot update assembly names(without .dll suffix)")]
        public string[] hotUpdateAssemblies;

        [Tooltip("preserved hot update assembly names(without .dll suffix)")]
        public string[] preserveHotUpdateAssemblies;

        [Tooltip("output directory of compiling hot update assemblies")]
        public string hotUpdateDllCompileOutputRootDir = "HybridCLRData/HotUpdateDlls";

        [Tooltip("searching paths of external hot update assemblies")]
        public string[] externalHotUpdateAssembliyDirs;

        [Tooltip("output directory of stripped AOT assemblies")]
        public string strippedAOTDllOutputRootDir = "HybridCLRData/AssembliesPostIl2CppStrip";

        [Tooltip("supplementary metadata assembly names(without .dll suffix)")]
        public string[] patchAOTAssemblies;

        [Tooltip("output file of automatic generated link.xml by scanning hot update assemblies")]
        public string outputLinkFile = "HybridCLRGenerate/link.xml";

        [Tooltip("output file of automatic generated AOTGenericReferences.cs")]
        public string outputAOTGenericReferenceFile = "HybridCLRGenerate/AOTGenericReferences.cs";

        [Tooltip("max iteration count of searching generic methods in hot update assemblies")]
        public int maxGenericReferenceIteration = 10;

        [Tooltip("max iteration count of searching method bridge generic methods in AOT assemblies")]
        public int maxMethodBridgeGenericIteration = 10;
    }
}
