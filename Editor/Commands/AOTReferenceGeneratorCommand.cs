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

﻿using HybridCLR.CodeGen.AOT;
using HybridCLR.Meta;

using HybridCLR.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HybridCLR.Commands
{
    public static class AOTReferenceGeneratorCommand
    {

        [MenuItem("HybridCLR/Generate/AOTGenericReference", priority = 102)]
        public static void CompileAndGenerateAOTGenericReference()
        {
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            CompileDllCommand.CompileDll(target);
            GenerateAOTGenericReference(target);
        }

        /// <summary>
        /// Calculate generic references in hot update code
        /// </summary>
        /// <param name="target"></param>
        public static void GenerateAOTGenericReference(BuildTarget target)
        {
            var gs = SettingsUtil.HybridCLRSettings;
            List<string> hotUpdateDllNames = SettingsUtil.HotUpdateAssemblyNamesExcludePreserved;

            AssemblyReferenceDeepCollector collector = new AssemblyReferenceDeepCollector(MetaUtil.CreateHotUpdateAndAOTAssemblyResolver(target, hotUpdateDllNames), hotUpdateDllNames);
            var analyzer = new AOTReferenceAnalyzer(new AOTReferenceAnalyzer.Options
            {
                MaxIterationCount = Math.Min(20, gs.maxGenericReferenceIteration),
                Collector = collector,
            });

            analyzer.Run();

            var writer = new AOTGenericReferenceWriter();
            writer.Write(analyzer.AotGenericTypes.ToList(), analyzer.AotGenericMethods.ToList(), $"{Application.dataPath}/{gs.outputAOTGenericReferenceFile}");
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Calculate generic references in hot update code, but exclude existing generic references in AOT
        /// </summary>
        /// <param name="target"></param>
        /// 
        public static void GeneratedAOTGenericReferenceExcludeExistsAOTClassAndMethods(BuildTarget target)
        {

            var gs = SettingsUtil.HybridCLRSettings;
            List<string> hotUpdateDllNames = SettingsUtil.HotUpdateAssemblyNamesExcludePreserved;

            AssemblyReferenceDeepCollector hotUpdateCollector = new AssemblyReferenceDeepCollector(MetaUtil.CreateHotUpdateAndAOTAssemblyResolver(target, hotUpdateDllNames), hotUpdateDllNames);
            var hotUpdateAnalyzer = new AOTReferenceAnalyzer(new AOTReferenceAnalyzer.Options
            {
                MaxIterationCount = Math.Min(10, gs.maxGenericReferenceIteration),
                Collector = hotUpdateCollector,
            });

            hotUpdateAnalyzer.Run();


            string aotDllDir = SettingsUtil.GetAssembliesPostIl2CppStripDir(target);
            List<string> aotAssemblyNames = Directory.Exists(aotDllDir) ?
                Directory.GetFiles(aotDllDir, "*.dll", SearchOption.TopDirectoryOnly).Select(Path.GetFileNameWithoutExtension).ToList()
                : new List<string>();
            if (aotAssemblyNames.Count == 0)
            {
                throw new Exception($"no aot assembly found. please run `HybridCLR/Generate/All` or `HybridCLR/Generate/AotDlls` to generate aot dlls before runing `HybridCLR/Generate/AOTGenericReference`");
            }
            AssemblyReferenceDeepCollector aotCollector = new AssemblyReferenceDeepCollector(MetaUtil.CreateAOTAssemblyResolver(target), aotAssemblyNames);
            var aotAnalyzer = new AOTReferenceAnalyzer(new AOTReferenceAnalyzer.Options
            {
                MaxIterationCount = Math.Min(10, gs.maxGenericReferenceIteration),
                Collector = aotCollector,
                ComputeAotAssembly = true,
            });

            aotAnalyzer.Run();

            var (resultTypes, resultMethods) = ExcludeExistAOTGenericTypeAndMethodss(hotUpdateAnalyzer.AotGenericTypes.ToList(), hotUpdateAnalyzer.AotGenericMethods.ToList(), aotAnalyzer.AotGenericTypes.ToList(), aotAnalyzer.AotGenericMethods.ToList());
            var writer = new AOTGenericReferenceWriter();
            writer.Write(resultTypes, resultMethods, $"{Application.dataPath}/{gs.outputAOTGenericReferenceFile}");
            AssetDatabase.Refresh();
        }


        private static (List<GenericClass>, List<GenericMethod>) ExcludeExistAOTGenericTypeAndMethodss(List<GenericClass> hotUpdateTypes, List<GenericMethod> hotUpdateMethods, List<GenericClass> aotTypes, List<GenericMethod> aotMethods)
        {
            var types = new List<GenericClass>();

            var typeSig2Type = hotUpdateTypes.ToDictionary(t => t.Type.DefinitionAssembly.Name + ":" + t.ToTypeSig(), t => t);
            foreach (var t in aotTypes)
            {
                string key = t.Type.DefinitionAssembly.Name + ":" + t.ToTypeSig();
                if (typeSig2Type.TryGetValue(key, out var removedType))
                {
                    typeSig2Type.Remove(key);
                    Debug.Log($"remove AOT type:{removedType.ToTypeSig()} ");
                }
            }

            var methodSig2Method = hotUpdateMethods.ToDictionary(m => m.Method.DeclaringType.DefinitionAssembly.Name + ":" + m.ToMethodSpec().ToString(), m => m);
            foreach (var m in aotMethods)
            {
                string key = m.Method.DeclaringType.DefinitionAssembly.Name + ":" + m.ToMethodSpec().ToString();
                if (methodSig2Method.TryGetValue(key, out var removedMethod))
                {
                    methodSig2Method.Remove(key);
                    Debug.Log($"remove AOT method:{removedMethod.ToMethodSpec()} ");
                }
            }

            return (typeSig2Type.Values.ToList(), methodSig2Method.Values.ToList());
        }
    }
}
