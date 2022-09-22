using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HybridCLR.Editor.Meta
{
    public class UnityEditorAssemblyResolver : IAssemblyResolver
    {
        public string ResolveAssembly(string assemblyName, bool throwExIfNotFind)
        {
            var refAss = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == assemblyName);
            if (refAss != null)
            {
                return refAss.Location;
            }
            return Assembly.Load(assemblyName).Location;
        }
    }
}
