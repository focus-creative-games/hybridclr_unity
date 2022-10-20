﻿using HybridCLR.Editor.ABI;
using HybridCLR.Editor.Link;
using HybridCLR.Editor.Meta;
using HybridCLR.Editor.ReversePInvokeWrap;
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
                analyzer.Run();


                string templateCode = File.ReadAllText($"{SettingsUtil.TemplatePathInPackage}/ReversePInvokeMethodStub.cpp");
                foreach (PlatformABI abi in Enum.GetValues(typeof(PlatformABI)))
                {
                    string outputFile = $"{SettingsUtil.GeneratedCppDir}/ReversePInvokeMethodStub_{abi}.cpp";

                    List<ABIReversePInvokeMethodInfo> methods = analyzer.BuildABIMethods(abi);
                    Debug.Log($"GenerateReversePInvokeWrapper. abi:{abi} wraperCount:{methods.Sum(m => m.Count)} output:{outputFile}");
                    var generator = new Generator();
                    generator.Generate(templateCode, abi, methods, outputFile);
                }
            }
            MethodBridgeGeneratorCommand.CleanIl2CppBuildCache();
        }
    }
}
