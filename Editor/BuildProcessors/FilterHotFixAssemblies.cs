using HybridCLR.Editor.Meta;
using System;
using System.Collections.Generic;
using System.IO;
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
            List<string> allHotUpdateDllNames = SettingsUtil.HotUpdateAssemblyNamesExcludePreserved;

            // 检查是否重复填写
            var hotUpdateDllSet = new HashSet<string>();
            foreach(var hotUpdateDll in allHotUpdateDllNames)
            {
                if (string.IsNullOrWhiteSpace(hotUpdateDll))
                {
                    throw new BuildFailedException($"热更新 assembly 名不能为空");
                }
                if (!hotUpdateDllSet.Add(hotUpdateDll))
                {
                    throw new BuildFailedException($"热更新 assembly:{hotUpdateDll} 在列表中重复，请除去重复条目");
                }
            }

            var assResolver = MetaUtil.CreateHotUpdateAssemblyResolver(EditorUserBuildSettings.activeBuildTarget, allHotUpdateDllNames);
            // 检查是否填写了正确的dll名称
            foreach (var hotUpdateDllName in allHotUpdateDllNames)
            {
                if (assemblies.Select(Path.GetFileNameWithoutExtension).All(ass => ass != hotUpdateDllName) 
                    && string.IsNullOrEmpty(assResolver.ResolveAssembly(hotUpdateDllName, false)))
                {
                    throw new BuildFailedException($"热更新 assembly:{hotUpdateDllName} 不存在，请检查拼写错误");
                }
            }

            // 将热更dll从打包列表中移除
            return assemblies.Where(ass =>
            {
                string assName = Path.GetFileNameWithoutExtension(ass);
                bool reserved = allHotUpdateDllNames.All(dll => !assName.Equals(dll, StringComparison.Ordinal));
                if (!reserved)
                {
                    Debug.Log($"[FilterHotFixAssemblies] 过滤热更新assembly:{assName}");
                }
                return reserved;
            }).ToArray();
        }
    }
}
