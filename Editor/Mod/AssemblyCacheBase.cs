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

﻿using dnlib.DotNet;
using HybridCLR.Utils;
using System.Collections.Generic;
using System.IO;

namespace HybridCLR.Mod
{
    public abstract class AssemblyCacheBase
    {
        private readonly IAssemblyResolver _assemblyPathResolver;
        private readonly ModuleContext _modCtx;
        private readonly AssemblyResolver _asmResolver;
        private bool _loadedNetstandard;


        public ModuleContext ModCtx => _modCtx;

        public Dictionary<string, ModuleDefMD> LoadedModules { get; } = new Dictionary<string, ModuleDefMD>();

        private readonly List<ModuleDefMD> _loadedModulesIncludeNetstandard = new List<ModuleDefMD>();

        protected AssemblyCacheBase(IAssemblyResolver assemblyResolver)
        {
            _assemblyPathResolver = assemblyResolver;
            _modCtx = ModuleDef.CreateModuleContext();
            _asmResolver = (AssemblyResolver)_modCtx.AssemblyResolver;
            _asmResolver.EnableTypeDefCache = true;
            _asmResolver.UseGAC = false;
        }


        public ModuleDefMD TryLoadModule(string moduleName, bool loadReferenceAssemblies = true)
        {
            string dllPath = _assemblyPathResolver.ResolveAssembly(moduleName, false);
            if (string.IsNullOrEmpty(dllPath))
            {
                return null;
            }
            return LoadModule(moduleName, loadReferenceAssemblies);
        }

        public ModuleDefMD LoadModule(string moduleName, bool loadReferenceAssemblies = true)
        {
            // Debug.Log($"load module:{moduleName}");
            if (LoadedModules.TryGetValue(moduleName, out var mod))
            {
                return mod;
            }
            if (moduleName == "netstandard")
            {
                if (!_loadedNetstandard)
                {
                    LoadNetStandard();
                }
                return null;
            }
            mod = DoLoadModule(_assemblyPathResolver.ResolveAssembly(moduleName, true));
            LoadedModules.Add(moduleName, mod);

            if (loadReferenceAssemblies)
            {
                foreach (var refAsm in mod.GetAssemblyRefs())
                {
                    LoadModule(refAsm.Name);
                }
            }

            return mod;
        }

        private void LoadNetStandard()
        {
            string netstandardDllPath = _assemblyPathResolver.ResolveAssembly("netstandard", false);
            if (!string.IsNullOrEmpty(netstandardDllPath))
            {
                DoLoadModule(netstandardDllPath);
            }
            else
            {
                DoLoadModule(MetaUtil.ResolveNetStandardAssemblyPath("netstandard2.0"));
                DoLoadModule(MetaUtil.ResolveNetStandardAssemblyPath("netstandard2.1"));
            }
            _loadedNetstandard = true;
        }

        private ModuleDefMD DoLoadModule(string dllPath)
        {
            //Debug.Log($"do load module:{dllPath}");
            ModuleDefMD mod = ModuleDefMD.Load(File.ReadAllBytes(dllPath), _modCtx);
            mod.EnableTypeDefFindCache = true;
            _asmResolver.AddToCache(mod);
            _loadedModulesIncludeNetstandard.Add(mod);
            return mod;
        }
    }
}
