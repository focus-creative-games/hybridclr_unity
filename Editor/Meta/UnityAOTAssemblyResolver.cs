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
    public class UnityAOTAssemblyResolver : IAssemblyResolver
    {
        public string ResolveAssembly(string assemblyName, bool throwExIfNotFind)
        {
            // 
            var assemblyFile = $"{UnityEditor.EditorApplication.applicationContentsPath}/MonoBleedingEdge/lib/mono/unityaot/{assemblyName}.dll";
            if (File.Exists(assemblyFile))
            {
                Debug.Log($"UnityAOTAssemblyResolver.ResolveAssembly:{assemblyFile}");
                return assemblyFile;
            }
            if (throwExIfNotFind)
            {
                throw new Exception($"resolve assembly:{assemblyName} fail");
            }
            return null;
        }
    }
}
