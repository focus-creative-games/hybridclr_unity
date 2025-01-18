using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using dnlib.DotNet;
using HybridCLR.Editor.Meta;
using UnityEditor;
using UnityEngine;
using IAssemblyResolver = HybridCLR.Editor.Meta.IAssemblyResolver;

namespace HybridCLR.Editor.Link
{
    public class Analyzer
    {
        private readonly IAssemblyResolver _resolver;

        public Analyzer(IAssemblyResolver resolver)
        {
            _resolver = resolver;
        }


        public HashSet<ITypeDefOrRef> CollectRefs(List<string> rootAssemblies, bool includeUnityEngineCoreTypes)
        {
            var typeRefs = new HashSet<ITypeDefOrRef>(TypeEqualityComparer.Instance);
            CollectHotUpdateRefs(rootAssemblies, typeRefs);

            if (includeUnityEngineCoreTypes)
            {
                CollectUnityEngineCoreTypes(typeRefs);
            }
            return typeRefs;
        }

        private void CollectUnityEngineCoreTypes(HashSet<ITypeDefOrRef> preservedTypes)
        {
            Debug.Log("CollectUnityEngineCoreTypes");
            string unityEngineDllPath = $"{EditorApplication.applicationContentsPath}/Managed/UnityEngine";

            var assCollector = new AssemblyCache(_resolver);
            foreach (string unityEngineDll in Directory.GetFiles(unityEngineDllPath, "UnityEngine.*.dll", SearchOption.AllDirectories))
            {
                // because of bug of unity 2019.4.x, preserving types in this dll will cause crash in android build
                if (unityEngineDll.EndsWith("UnityEngine.PerformanceReportingModule.dll", StringComparison.InvariantCulture))
                {
                    continue;
                }
                ModuleDefMD mod = assCollector.LoadModuleFromFileWithoutCache(unityEngineDll);
                foreach (TypeDef type in mod.GetTypes())
                {
                    if (type.Methods.Any(m => m.IsInternalCall || m.IsPinvokeImpl))
                    {
                        preservedTypes.Add(type);
                    }
                }
            }
        }

        public void CollectHotUpdateRefs(List<string> rootAssemblies, HashSet<ITypeDefOrRef> preservedTypes)
        {
            var assCollector = new AssemblyCache(_resolver);
            var rootAssemblyNames = new HashSet<string>(rootAssemblies);

            foreach (var rootAss in rootAssemblies)
            {
                var dnAss = assCollector.LoadModule(rootAss, false);
                foreach (var type in dnAss.GetTypeRefs())
                {
                    if (type.DefinitionAssembly == null)
                    {
                        Debug.LogWarning($"assembly:{dnAss.Name} TypeRef {type.FullName} has no DefinitionAssembly");
                        continue;
                    }
                    if (!rootAssemblyNames.Contains(type.DefinitionAssembly.Name.ToString()))
                    {
                        preservedTypes.Add(type);
                    }
                }
            }
        }
    }
}
