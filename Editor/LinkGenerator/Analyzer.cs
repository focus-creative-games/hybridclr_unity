using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using dnlib.DotNet;
using HybridCLR.Editor.Meta;
using IAssemblyResolver = HybridCLR.Editor.Meta.IAssemblyResolver;

namespace HybridCLR.Editor.LinkGenerator
{
    class Analyzer
    {
        private readonly IAssemblyResolver _resolver;

        public Analyzer(IAssemblyResolver resolver)
        {
            _resolver = resolver;
        }

        public HashSet<TypeRef> CollectRefs(List<Assembly> rootAssemblies)
        {
            var assCollector = new AssemblyCache(_resolver);
            var rootAssemblyName = new HashSet<string>();
            foreach(var ass in rootAssemblies)
            {
                if (!rootAssemblyName.Add(ass.GetName().Name))
                {
                    throw new Exception($"assembly:{ass.GetName().Name} 重复");
                }
            }

            var typeRefs = new HashSet<TypeRef>(TypeEqualityComparer.Instance);
            foreach (var rootAss in rootAssemblies)
            {
                var dnAss = assCollector.LoadModule(rootAss.GetName().Name);
                foreach(var type in dnAss.GetTypeRefs())
                {
                    if (!rootAssemblyName.Contains(type.DefinitionAssembly.Name))
                    {
                        typeRefs.Add(type);
                    }
                }
            }
            return typeRefs;
        }
    }
}
