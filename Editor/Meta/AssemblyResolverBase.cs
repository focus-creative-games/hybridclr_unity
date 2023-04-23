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
                if (SettingsUtil.HotUpdateAssemblyNamesIncludePreserved.Contains(assemblyName))
                {
                    throw new Exception($"resolve 热更新 dll:{assemblyName} 失败！请确保已经编译了热更新dll或者外部热更新路径中配置了正确的值。更多请参阅常见错误文档");
                }
                else
                {
                    throw new Exception($"resolve AOT dll:{assemblyName} 失败! 请确保主工程已经引用了该dll并且正确生成了裁剪后的AOT dll。更多请参阅常见错误文档");
                }
            }
            return null;
        }

        protected abstract bool TryResolveAssembly(string assemblyName, out string assemblyPath);
    }
}
