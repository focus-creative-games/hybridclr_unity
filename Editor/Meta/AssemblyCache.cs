using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace HybridCLR.Editor.Meta
{
    public class AssemblyCache
    {
        private readonly IAssemblyResolver _assemblyPathResolver;
        private readonly ModuleContext _modCtx;
        private readonly AssemblyResolver _asmResolver;

        public Dictionary<string, ModuleDefMD> LoadedModules { get; } = new Dictionary<string, ModuleDefMD>();

        public AssemblyCache(IAssemblyResolver assemblyResolver)
        {
            _assemblyPathResolver = assemblyResolver;
            _modCtx = ModuleDef.CreateModuleContext();
            _asmResolver = (AssemblyResolver)_modCtx.AssemblyResolver;
            _asmResolver.EnableTypeDefCache = true;
            _asmResolver.UseGAC = false;
        }

        public ModuleDefMD LoadModule(string moduleName)
        {
            // Debug.Log($"load module:{moduleName}");
            if (LoadedModules.TryGetValue(moduleName, out var mod))
            {
                return mod;
            }
            mod = DoLoadModule(_assemblyPathResolver.ResolveAssembly(moduleName, true));
            LoadedModules.Add(moduleName, mod);

            foreach (var refAsm in mod.GetAssemblyRefs())
            {
                LoadModule(refAsm.Name);
            }
            return mod;
        }

        private ModuleDefMD DoLoadModule(string dllPath)
        {
            //Debug.Log($"do load module:{dllPath}");
            ModuleDefMD mod = ModuleDefMD.Load(dllPath, _modCtx);
            _asmResolver.AddToCache(mod);
            return mod;
        }

    }
}
