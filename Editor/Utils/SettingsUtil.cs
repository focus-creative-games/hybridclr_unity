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

﻿using HybridCLR.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;


namespace HybridCLR
{
    public static class SettingsUtil
    {


        private const string HybridCLRDataPath = "Library/HybridCLR";
        private const string CompiledDllOutputDirName = "HotUpdateDlls";
        private const string StripedAotDllOutputDirName = "AssembliesPostIl2CppStrip";

        public static string ProjectDir { get; } = Directory.GetParent(Application.dataPath).ToString();

        public static string PackageName { get; } = "com.code-philosophy.hybridclr";

        public static string HybridCLRDataPathInPackage => $"Packages/{PackageName}/Data~";

        public static string TemplatePathInPackage => $"{HybridCLRDataPathInPackage}/Templates";



        public static string HybridCLRDataDir => $"{ProjectDir}/{HybridCLRDataPath}";

        public static string LocalUnityDataDir => $"{HybridCLRDataDir}/LocalIl2CppData-{Application.platform}";

        public static string LocalIl2CppDir => $"{LocalUnityDataDir}/il2cpp";

        public static string GeneratedCppDir => $"{LocalIl2CppDir}/libil2cpp/hybridclr/generated";

        public static string GetHotUpdateDllsOutputDirByTarget(BuildTarget target)
        {
            return $"{HybridCLRDataDir}/{CompiledDllOutputDirName}/{target}";
        }

        public static string GetAssembliesPostIl2CppStripDir(BuildTarget target)
        {
            return $"{HybridCLRDataDir}/{StripedAotDllOutputDirName}/{target}";
        }

        public static bool Enable
        {
            get => HybridCLRSettings.instance.enable;
            set
            {
                HybridCLRSettings.instance.enable = value;
                HybridCLRSettings.Save();
            }
        }

        class AssemblyDefinitionData
        {
            public string name;
        }

        public static List<string> HotUpdateAssemblyNamesExcludePreserved
        {
            get
            {
                var gs = HybridCLRSettings.instance;
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

        public static List<string> HotUpdateAssemblyFilesExcludePreserved => HotUpdateAssemblyNamesExcludePreserved.Select(dll => dll + ".dll").ToList();


        public static List<string> HotUpdateAssemblyNamesIncludePreserved
        {
            get
            {
                List<string> allAsses = HotUpdateAssemblyNamesExcludePreserved;
                string[] preserveAssemblyNames = HybridCLRSettings.instance.preserveHotUpdateAssemblies;
                if (preserveAssemblyNames != null && preserveAssemblyNames.Length > 0)
                {
                    foreach (var assemblyName in preserveAssemblyNames)
                    {
                        if (allAsses.Contains(assemblyName))
                        {
                            throw new Exception($"[HotUpdateAssemblyNamesIncludePreserved] assembly:'{assemblyName}' is duplicated");
                        }
                        allAsses.Add(assemblyName);
                    }
                }

                return allAsses;
            }
        }

        public static List<string> HotUpdateAssemblyFilesIncludePreserved => HotUpdateAssemblyNamesIncludePreserved.Select(ass => ass + ".dll").ToList();

        public static HybridCLRSettings HybridCLRSettings => HybridCLRSettings.instance;
    }
}
