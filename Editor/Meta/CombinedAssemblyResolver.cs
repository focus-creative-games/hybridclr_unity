using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HybridCLR.Editor.Meta
{
    public class CombinedAssemblyResolver : IAssemblyResolver
    {
        private readonly IAssemblyResolver[] _resolvers;

        public CombinedAssemblyResolver(params IAssemblyResolver[] resolvers)
        {
            _resolvers = resolvers;
        }

        public string ResolveAssembly(string assemblyName, bool throwExIfNotFind)
        {
            foreach(var resolver in _resolvers)
            {
                var assembly = resolver.ResolveAssembly(assemblyName, false);
                if (assembly != null)
                {
                    return assembly;
                }
            }
            if (throwExIfNotFind)
            {
                throw new Exception($"resolve assembly:{assemblyName} fail");
            }
            return null;
        }
    }
}
