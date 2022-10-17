using HybridCLR.Editor.ABI;
using HybridCLR.Editor.Link;
using HybridCLR.Editor.Meta;
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

    public static class ReversePInvokeWrapperGeneratorCommand
    {

        [MenuItem("HybridCLR/Generate/ReversePInvokeWrapper", priority = 103)]
        public static void GenerateReversePInvokeWrapper()
        {
            CompileDllCommand.CompileDllActiveBuildTarget();
            using (var cache = new AssemblyCache(MetaUtil.CreateBuildTargetAssemblyResolver(EditorUserBuildSettings.activeBuildTarget)))
            {
                var analyzer = new ReversePInvokeWrap.Analyzer(cache, SettingsUtil.HotUpdateAssemblyNames);
                var methods = analyzer.CollectMonoPInvokeCallbackMethods();
                foreach (var method in methods)
                {
                    Debug.Log($"method:{method.Method}");
                }

                var generateJobs = new List<(PlatformABI, string)>()
                {
                    (PlatformABI.Arm64, "HYBRIDCLR_ABI_ARM_64"),
                    (PlatformABI.Universal64, "HYBRIDCLR_ABI_UNIVERSAL_64"),
                    (PlatformABI.Universal32, "HYBRIDCLR_ABI_UNIVERSAL_32"),
                };
            }
            return;

            //string ReversePInvokeWrapperStubFile = $"{SettingsUtil.LocalIl2CppDir}/libil2cpp/hybridclr/metadata/ReversePInvokeMethodStub.cpp";
            //string wrapperTemplateStr = File.ReadAllText($"{SettingsUtil.TemplatePathInPackage}/ReversePInvokeMethodStub.cpp.txt");
            //int wrapperCount = SettingsUtil.HybridCLRSettings.ReversePInvokeWrapperCount;
            //var generator = new Generator();
            //generator.Generate(wrapperTemplateStr, wrapperCount,ReversePInvokeWrapperStubFile);
            //Debug.Log($"GenerateReversePInvokeWrapper. wraperCount:{wrapperCount} output:{ReversePInvokeWrapperStubFile}");
            //MethodBridgeGeneratorCommand.CleanIl2CppBuildCache();
        }
    }
}
