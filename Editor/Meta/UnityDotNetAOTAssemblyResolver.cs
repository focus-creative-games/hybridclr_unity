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
    public class UnityDotNetAOTAssemblyResolver : IAssemblyResolver
    {
        public string ResolveAssembly(string assemblyName, bool throwExIfNotFind)
        {
            // 
#if !UNITY_2021_1_OR_NEWER
            var assemblyFile = $"{UnityEditor.EditorApplication.applicationContentsPath}/MonoBleedingEdge/lib/mono/unityaot/{assemblyName}.dll";
#else
#if UNITY_STANDALONE_WIN
            var assemblyFile = $"{UnityEditor.EditorApplication.applicationContentsPath}/MonoBleedingEdge/lib/mono/unityaot-win32/{assemblyName}.dll";
#elif UNITY_STANDALONE_OSX || UNITY_IOS
            var assemblyFile = $"{UnityEditor.EditorApplication.applicationContentsPath}/MonoBleedingEdge/lib/mono/unityaot-macos/{assemblyName}.dll";
#elif UNITY_STANDALONE_LINUX || UNITY_ANDROID
            var assemblyFile = $"{UnityEditor.EditorApplication.applicationContentsPath}/MonoBleedingEdge/lib/mono/unityaot-linux/{assemblyName}.dll";
#else
            var assemblyFile = $"{UnityEditor.EditorApplication.applicationContentsPath}/MonoBleedingEdge/lib/mono/unityaot-linux/{assemblyName}.dll";
#endif
#endif

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
