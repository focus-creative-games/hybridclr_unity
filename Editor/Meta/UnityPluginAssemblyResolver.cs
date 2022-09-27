using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace HybridCLR.Editor.Meta
{
    public class UnityPluginAssemblyResolver : IAssemblyResolver
    {
        public string ResolveAssembly(string assemblyName, bool throwExIfNotFind)
        {
            foreach(var dll in Directory.GetFiles(UnityEngine.Application.dataPath, "*.dll", SearchOption.AllDirectories))
            {
                if (Path.GetFileNameWithoutExtension(dll) == assemblyName && dll.Contains("Plugins"))
                {
                    Debug.Log($"plugin:{dll}");
                    return dll;
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
