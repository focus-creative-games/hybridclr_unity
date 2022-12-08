using HybridCLR.Editor;
using HybridCLR.Editor.ABI;
using HybridCLR.Editor.Meta;
using HybridCLR.Editor.MethodBridge;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace HybridCLR.Editor.Commands
{
    public class MethodBridgeGeneratorCommand
    {

        public static void CleanIl2CppBuildCache()
        {
            string il2cppBuildCachePath = SettingsUtil.Il2CppBuildCacheDir;
            if (!Directory.Exists(il2cppBuildCachePath))
            {
                return;
            }
            Debug.Log($"clean il2cpp build cache:{il2cppBuildCachePath}");
            Directory.Delete(il2cppBuildCachePath, true);
        }

        private static void GenerateMethodBridgeCppFile(Analyzer analyzer, PlatformABI platform, string templateCode, string outputFile)
        {
            var g = new Generator(new Generator.Options()
            {
                PlatformABI = platform,
                TemplateCode = templateCode,
                OutputFile = outputFile,
                GenericMethods = analyzer.GenericMethods,
                NotGenericMethods = analyzer.NotGenericMethods,
            });

            g.PrepareMethods();
            g.Generate();
            Debug.LogFormat("== output:{0} ==", outputFile);
        }

        [MenuItem("HybridCLR/Generate/MethodBridge", priority = 101)]
        public static void GenerateMethodBridge()
        {
            GenerateMethodBridge(true);
        }

        public static void GenerateMethodBridge(bool compileDll)
        {
            // 此处理论会有点问题，打每个平台的时候，都得针对当前平台生成桥接函数
            // 但影响不大，先这样吧
            if (compileDll)
            {
                CompileDllCommand.CompileDllActiveBuildTarget();
            }

            using (AssemblyReferenceDeepCollector collector = new AssemblyReferenceDeepCollector(MetaUtil.CreateBuildTargetAssemblyResolver(EditorUserBuildSettings.activeBuildTarget), SettingsUtil.HotUpdateAssemblyNames))
            {
                var analyzer = new Analyzer(new Analyzer.Options
                {
                    MaxIterationCount = Math.Min(20, SettingsUtil.HybridCLRSettings.maxMethodBridgeGenericIteration),
                    Collector = collector,
                });

                analyzer.Run();

                var tasks = new List<Task>();
                string templateCode = File.ReadAllText($"{SettingsUtil.TemplatePathInPackage}/MethodBridgeStub.cpp");
                foreach (PlatformABI platform in Enum.GetValues(typeof(PlatformABI)))
                {
                    string outputFile = $"{SettingsUtil.GeneratedCppDir}/MethodBridge_{platform}.cpp";
                    tasks.Add(Task.Run(() =>
                    {
                        GenerateMethodBridgeCppFile(analyzer, platform, templateCode, outputFile);
                    }));
                }
                Task.WaitAll(tasks.ToArray());
            }

            CleanIl2CppBuildCache();
        }
    }
}
