using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace HybridCLR.Editor
{
    public static class SettingsUtil
    {
        public static bool Enable => GlobalSettings.enable;

        public static string PackageName { get; } = "com.focus-creative-games.hybridclr_unity";

        public static string HybridCLRDataPathInPackage => $"Packages/{PackageName}/Data~";

        public static string HybridCLRDataPathInProject => "Assets/HybridCLRData";

        public static string TemplatePathInPackage => $"{HybridCLRDataPathInPackage}/Templates";

        public static string ProjectDir { get; } = Directory.GetParent(Application.dataPath).ToString();

        public static string ScriptingAssembliesJsonFile { get; } = "ScriptingAssemblies.json";

        public static string GlobalgamemanagersBinFile { get; } = "globalgamemanagers";

        public static string Dataunity3dBinFile { get; } = "data.unity3d";

        public static string HotFixDllsOutputDir => $"{HybridCLRDataDir}/{GlobalSettings.hotUpdateDllOutputDir}";

        public static string HybridCLRDataDir => $"{ProjectDir}/{GlobalSettings.hybridCLRDataDir}";

        public static string AssembliesPostIl2CppStripDir => $"{HybridCLRDataDir}/{GlobalSettings.strippedAssemblyDir}";

        public static string LocalUnityDataDir => $"{HybridCLRDataDir}/LocalIl2CppData-{Application.platform}";

        public static string LocalIl2CppDir => $"{LocalUnityDataDir}/il2cpp";

        public static string MethodBridgeCppDir => $"{LocalIl2CppDir}/libil2cpp/hybridclr/interpreter";

        public static string Il2CppBuildCacheDir { get; } = $"{ProjectDir}/Library/Il2cppBuildCache";

        public static string GetHotFixDllsOutputDirByTarget(BuildTarget target)
        {
            return $"{HotFixDllsOutputDir}/{target}";
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
                var gs = GlobalSettings;
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

        public static T GetSingletonAssets<T>() where T : ScriptableObject
        {
            string assetPath = $"{HybridCLRDataPathInProject}/{typeof(T).Name}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);                                                                                                                                                                 
            if (!asset)
            {
                string fullPath = Path.GetFullPath(HybridCLRDataPathInProject);
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                }
                Debug.LogWarning($"没找到 {typeof(T).Name} asset，自动创建创建一个:{assetPath}.");

                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }
            return asset;
        }

        [MenuItem("Assets/Create/HybridCLR", menuItem = "Assets/Create/HybridCLR/GlobalSettings", priority = 90)]
        static void CreateOrLoadHybridCLRSettings()
        {
            var settings = GetSingletonAssets<HybridCLRGlobalSettings>();
            EditorGUIUtility.PingObject(settings);
        }

        public static HybridCLRGlobalSettings GlobalSettings => GetSingletonAssets<HybridCLRGlobalSettings>();
    }
}
