using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace HybridCLR.Editor.BuildProcessors
{
    /// <summary>
    /// 将热更新dll从Build过程中过滤，防止打包到主工程中
    /// </summary>
    internal class FilterHotFixAssemblies : IFilterBuildAssemblies
    {
        public int callbackOrder => 0;

        public string[] OnFilterAssemblies(BuildOptions buildOptions, string[] assemblies)
        {
            if (!SettingsUtil.Enable)
            {
                Debug.Log($"[FilterHotFixAssemblies] disabled");
                return assemblies;
            }
            List<string> allHotUpdateDllFiles = SettingsUtil.HotUpdateAssemblyFiles;

            // 检查是否重复填写
            var hotUpdateDllSet = new HashSet<string>();
            foreach(var hotUpdateDll in allHotUpdateDllFiles)
            {
                if (!hotUpdateDllSet.Add(hotUpdateDll))
                {
                    throw new Exception($"热更新 assembly:{hotUpdateDll} 在列表中重复，请除去重复条目");
                }
            }

            // 检查是否填写了正确的dll名称
            foreach (var hotUpdateDll in allHotUpdateDllFiles)
            {
                if (assemblies.All(ass => !ass.EndsWith(hotUpdateDll)))
                {
                    throw new Exception($"热更新 assembly:{hotUpdateDll} 不存在，请检查拼写错误");
                }
                Debug.Log($"[FilterHotFixAssemblies] 过滤热更新assembly:{hotUpdateDll}");
            }
            
            // 将热更dll从打包列表中移除
            return assemblies.Where(ass => allHotUpdateDllFiles.All(dll => !ass.EndsWith(dll, StringComparison.OrdinalIgnoreCase))).ToArray();
        }
    }
}
