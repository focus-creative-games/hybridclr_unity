using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HybridCLR.Editor.Meta
{
    public abstract class AssemblyResolverBase : IAssemblyResolver
    {
        public string ResolveAssembly(string assemblyName, bool throwExIfNotFind)
        {
            if (TryResolveAssembly(assemblyName, out string assemblyPath))
            {
                return assemblyPath;
            }
            if (throwExIfNotFind)
            {
                throw new Exception($"resolve assembly:{assemblyName} 失败! 如果是热更新dll找不到，请先运行`HybridCLR/CompileDll/ActiveBuildTarget`编译生成热更新dll。如果是AOT dll找不到，请先运行`HybridCLR/Generate/LinkXml`，接着在`Build Settings`中打包或者导出工程来生成AOT dll");
            }
            return null;
        }

        protected abstract bool TryResolveAssembly(string assemblyName, out string assemblyPath);
    }
}
