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
        private static readonly HashSet<string> s_ignoreDirs = new HashSet<string>
        {
            "Editor",
            "StreamingAssets",
            "Resources",
        };

        private bool IsPluginDll(string dllPath)
        {
            var fileOrDirs = new HashSet<string>(dllPath.Split('\\', '/'));
            return !fileOrDirs.Any(dir => dir.EndsWith("~")) && !fileOrDirs.Intersect(s_ignoreDirs).Any();
        }

        public string ResolveAssembly(string assemblyName, bool throwExIfNotFind)
        {
            foreach(var dll in Directory.GetFiles(UnityEngine.Application.dataPath, "*.dll", SearchOption.AllDirectories))
            {
                if (Path.GetFileNameWithoutExtension(dll) == assemblyName && IsPluginDll(dll))
                {
                    Debug.Log($"UnityPluginAssemblyResolver.ResolveAssembly:{dll}");
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
