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

﻿
using HybridCLR.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace HybridCLR.BuildProcessors
{
    /// <summary>
    /// Filter hot update assemblies from build process to prevent them from being packaged into the main project
    /// </summary>
    internal class FilterHotFixAssemblies : IFilterBuildAssemblies
    {
        public int callbackOrder => 0;

        public string[] OnFilterAssemblies(BuildOptions buildOptions, string[] assemblies)
        {
            if (!SettingsUtil.Enable)
            {
                Debug.Log($"[FilterHotFixAssemblies] disabled");
                return assemblies;
            }
            List<string> allHotUpdateDllNames = SettingsUtil.HotUpdateAssemblyNamesExcludePreserved;

            var hotUpdateDllSet = new HashSet<string>();
            foreach (var hotUpdateDll in allHotUpdateDllNames)
            {
                if (string.IsNullOrWhiteSpace(hotUpdateDll))
                {
                    throw new BuildFailedException($"hot update assembly name cann't be empty");
                }
                if (!hotUpdateDllSet.Add(hotUpdateDll))
                {
                    throw new BuildFailedException($"hot update assembly:{hotUpdateDll} is duplicated");
                }
            }

            var assResolver = MetaUtil.CreateHotUpdateAssemblyResolver(EditorUserBuildSettings.activeBuildTarget, allHotUpdateDllNames);
            foreach (var hotUpdateDllName in allHotUpdateDllNames)
            {
                if (assemblies.Select(Path.GetFileNameWithoutExtension).All(ass => ass != hotUpdateDllName)
                    && string.IsNullOrEmpty(assResolver.ResolveAssembly(hotUpdateDllName, false)))
                {
                    throw new BuildFailedException($"hot update assembly:{hotUpdateDllName} doesn't exist");
                }
            }

            return assemblies.Where(ass =>
            {
                string assName = Path.GetFileNameWithoutExtension(ass);
                bool reserved = allHotUpdateDllNames.All(dll => !assName.Equals(dll, StringComparison.Ordinal));
                if (!reserved)
                {
                    Debug.Log($"[FilterHotFixAssemblies] filter assembly:{assName}");
                }
                return reserved;
            }).ToArray();
        }
    }
}
