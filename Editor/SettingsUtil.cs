using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;


namespace HybridCLR.Editor
{
    public static class SettingsUtil
    {
        public static bool Enable => HybridCLRSettings.Instance.enable;

        public static string PackageName { get; } = "com.focus-creative-games.hybridclr_unity";

        public static string HybridCLRDataPathInPackage => $"Packages/{PackageName}/Data~";

        public static string TemplatePathInPackage => $"{HybridCLRDataPathInPackage}/Templates";

        public static string ProjectDir { get; } = Directory.GetParent(Application.dataPath).ToString();

        public static string ScriptingAssembliesJsonFile { get; } = "ScriptingAssemblies.json";

        public static string GlobalgamemanagersBinFile { get; } = "globalgamemanagers";

        public static string Dataunity3dBinFile { get; } = "data.unity3d";

        public static string HotUpdateDllsRootOutputDir => $"{HybridCLRDataDir}/HotUpdateDlls";

        public static string HybridCLRDataDir => $"{ProjectDir}/HybridCLRData";

        public static string AssembliesPostIl2CppStripDir => $"{HybridCLRDataDir}/AssembliesPostIl2CppStrip";

        public static string LocalUnityDataDir => $"{HybridCLRDataDir}/LocalIl2CppData-{Application.platform}";

        public static string LocalIl2CppDir => $"{LocalUnityDataDir}/il2cpp";

        public static string GeneratedCppDir => $"{LocalIl2CppDir}/libil2cpp/hybridclr/generated";

        public static string Il2CppBuildCacheDir { get; } = $"{ProjectDir}/Library/Il2cppBuildCache";

        public static string GetHotUpdateDllsOutputDirByTarget(BuildTarget target)
        {
            return $"{HotUpdateDllsRootOutputDir}/{target}";
        }

        public static string GetAssembliesPostIl2CppStripDir(BuildTarget target)
        {
            return $"{AssembliesPostIl2CppStripDir}/{target}";
        }

        class AssemblyDefinitionData
        {
            public string name;
        }

        /// <summary>
        /// 所有热更新dll列表。放到此列表中的dll在打包时OnFilterAssemblies回调中被过滤。
        /// </summary>
        public static List<string> HotUpdateAssemblyNames
        {
            get
            {
                var gs = HybridCLRSettings.Instance;
                var hotfixAssNames = (gs.hotUpdateAssemblyDefinitions ?? Array.Empty<AssemblyDefinitionAsset>()).Select(ad => JsonUtility.FromJson<AssemblyDefinitionData>(ad.text));

                var hotfixAssembles = new List<string>();
                foreach (var assName in hotfixAssNames)
                {
                    hotfixAssembles.Add(assName.name);
                }
                hotfixAssembles.AddRange(gs.hotUpdateAssemblies ?? Array.Empty<string>());
                return hotfixAssembles.ToList();
            }
        }
        public static List<string> HotUpdateAssemblyFiles => HotUpdateAssemblyNames.Select(dll => dll + ".dll").ToList();

        public static HybridCLRSettings HybridCLRSettings => HybridCLRSettings.Instance;
    }
}
