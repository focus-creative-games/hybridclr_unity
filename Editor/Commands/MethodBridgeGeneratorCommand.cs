// Copyright 2026 Code Philosophy
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

﻿using HybridCLR.Meta;
using HybridCLR.CodeGen.MethodBridge;
using HybridCLR.Mod;
using HybridCLR.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HybridCLR.Commands
{
    public class MethodBridgeGeneratorCommand
    {
        public static void GenerateMethodBridgeCppFile(IReadOnlyCollection<GenericMethod> genericMethods, List<RawMonoPInvokeCallbackMethodInfo> reversePInvokeMethods, IReadOnlyCollection<CallNativeMethodSignatureInfo> calliMethodSignatures, string outputFile)
        {
            var g = new Generator(new Generator.Options()
            {
                OutputFile = outputFile,
                GenericMethods = genericMethods,
                ReversePInvokeMethods = reversePInvokeMethods,
                CalliMethodSignatures = calliMethodSignatures,
                Development = EditorUserBuildSettings.development,
            });

            g.Generate();
            Debug.LogFormat("[MethodBridgeGeneratorCommand] output:{0}", outputFile);
        }

        [MenuItem("HybridCLR/Generate/MethodBridgeAndReversePInvokeWrapper", priority = 101)]
        public static void GenerateMethodBridgeAndReversePInvokeWrapper()
        {
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            GenerateMethodBridgeAndReversePInvokeWrapper(target);
        }

        public static void GenerateMethodBridgeAndReversePInvokeWrapper(BuildTarget target)
        {
            string aotDllDir = SettingsUtil.GetAssembliesPostIl2CppStripDir(target);
            List<string> aotAssemblyNames = Directory.Exists(aotDllDir) ?
                Directory.GetFiles(aotDllDir, "*.dll", SearchOption.TopDirectoryOnly).Select(Path.GetFileNameWithoutExtension).ToList()
                : new List<string>();
            if (aotAssemblyNames.Count == 0)
            {
                throw new Exception($"no aot assembly found. please run `HybridCLR/Generate/All` or `HybridCLR/Generate/AotDlls` to generate aot dlls before runing `HybridCLR/Generate/MethodBridge`");
            }
            AssemblyReferenceDeepCollector collector = new AssemblyReferenceDeepCollector(MetaUtil.CreateAOTAssemblyResolver(target), aotAssemblyNames);

            var methodBridgeAnalyzer = new MethodBridgeAnalyzer(new MethodBridgeAnalyzer.Options
            {
                MaxIterationCount = Math.Min(20, SettingsUtil.HybridCLRSettings.maxMethodBridgeGenericIteration),
                Collector = collector,
            });

            methodBridgeAnalyzer.Run();

            List<string> hotUpdateDlls = SettingsUtil.HotUpdateAssemblyNamesExcludePreserved;
            var cache = new AssemblyCache(MetaUtil.CreateHotUpdateAndAOTAssemblyResolver(target, hotUpdateDlls));

            var reversePInvokeAnalyzer = new MonoPInvokeCallbackAnalyzer(cache, hotUpdateDlls);
            reversePInvokeAnalyzer.Run();

            var calliAnalyzer = new CalliAnalyzer(cache, hotUpdateDlls);
            calliAnalyzer.Run();

            string outputFile = $"{SettingsUtil.GeneratedCppDir}/MethodBridge.inc";

            var callNativeMethodSignatures = calliAnalyzer.CalliMethodSignatures.ToList();
            GenerateMethodBridgeCppFile(methodBridgeAnalyzer.GenericMethods, reversePInvokeAnalyzer.ReversePInvokeMethods, callNativeMethodSignatures, outputFile);
        }
    }
}
