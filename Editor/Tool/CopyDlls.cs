using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HybridCLR.Editor.Settings;
using UnityEditor;
using UnityEngine;

namespace HybridCLR.Editor.Tool
{
    public static class CopyDlls
    {
        private static readonly Dictionary<string, string> CopyList = new Dictionary<string, string>();
        private static Assembly _assemblyCsharp;
        private static string _aotGenericReferenceFileName;
        private static object _aotGenericReferenceObj;

        /// <summary>
        /// 复制 AOT Dlls 到指定的文件夹，
        /// 复制的dll在 AOTGenericReferences.cs 中的 PatchedAOTAssemblyList 和 在设置面板指定的 Addition 共同决定, 二者重复无碍。
        /// </summary>
        public static void CopyAOTDlls()
        {
            var sourceDir = Path.Combine(SettingsUtil.ProjectDir, SettingsUtil.AssembliesPostIl2CppStripDir, EditorUserBuildSettings.activeBuildTarget.ToString());
            var targetDir = Path.Combine(SettingsUtil.ProjectDir, HybridCLRSettings.Instance.copyAOTDllsTargetDir, EditorUserBuildSettings.activeBuildTarget.ToString());

            CopyList.Clear();

            if (!Directory.Exists(sourceDir))
            {
                Debug.LogWarning("源 AOT Dll 目录不存在，请先执行 Generate/AOTDlls");
                return;
            }

            var outputAOTGenericReferenceFile = Path.Combine(Application.dataPath, HybridCLRSettings.Instance.outputAOTGenericReferenceFile);
            if (File.Exists(outputAOTGenericReferenceFile))
            {
                if (string.IsNullOrEmpty(_aotGenericReferenceFileName))
                {
                    _aotGenericReferenceFileName = HybridCLRSettings.Instance.outputAOTGenericReferenceFile.Split('/')[^1].Split('.')[0];
                }

                try
                {
                    if (_assemblyCsharp == null) _assemblyCsharp = Assembly.LoadFile(Path.Combine(SettingsUtil.ProjectDir, "Library", "ScriptAssemblies", "Assembly-CSharp.dll"));
                    foreach (var type in _assemblyCsharp.GetTypes())
                    {
                        if (type.Name.Equals(_aotGenericReferenceFileName))
                        {
                            var fi = type.GetField("PatchedAOTAssemblyList", BindingFlags.Public | BindingFlags.Static);
                            if (fi != null)
                            {
                                _aotGenericReferenceObj ??= Activator.CreateInstance(type);
                                if (_aotGenericReferenceObj != null)
                                {
                                    var value = fi.GetValue(_aotGenericReferenceObj);
                                    foreach (var str in (IReadOnlyList<string>)value)
                                    {
                                        CopyList[str] = Path.Combine(sourceDir, str);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.Log($"加载 Assembly-CSharp.dll 异常: {e.Message}");
                    throw;
                }
            }
            else
            {
                Debug.LogWarning("未检测到 AOTGenericReferences.cs 文件，仅复制 Addition 部分");
            }

            if (HybridCLRSettings.Instance.copyAOTDllsAddition != null)
            {
                foreach (var addition in HybridCLRSettings.Instance.copyAOTDllsAddition)
                {
                    if (string.IsNullOrEmpty(addition)) continue;
                    CopyList[addition + ".dll"] = Path.Combine(sourceDir, addition + ".dll");
                }
            }

            Copy(targetDir, (str) => { Debug.LogWarning($"{str} 不存在，复制时被跳过"); }, (str) => { Debug.Log($"成功复制AOTDll: {str}"); });
        }

        public static void CopyHotUpdateDlls()
        {
            var sourceDir = Path.Combine(SettingsUtil.ProjectDir, SettingsUtil.HotUpdateDllsRootOutputDir, EditorUserBuildSettings.activeBuildTarget.ToString());
            string targetDir;
            if (HybridCLRSettings.Instance.ignorePlatformWhenCopyHotUpdateDll)
            {
                targetDir = Path.Combine(SettingsUtil.ProjectDir, HybridCLRSettings.Instance.copyHotUpdateDllsTargetDir);
            }
            else
            {
                targetDir = Path.Combine(SettingsUtil.ProjectDir, HybridCLRSettings.Instance.copyHotUpdateDllsTargetDir, EditorUserBuildSettings.activeBuildTarget.ToString());
            }

            CopyList.Clear();

            if (!Directory.Exists(sourceDir))
            {
                Debug.LogError("源 HotUpdate Dll 目录不存在，请先执行 CompileDll");
                return;
            }

            foreach (var definition in HybridCLRSettings.Instance.hotUpdateAssemblyDefinitions)
            {
                CopyList[definition.name + ".dll"] = Path.Combine(sourceDir, definition.name + ".dll");
            }

            foreach (var assembly in HybridCLRSettings.Instance.hotUpdateAssemblies)
            {
                if (string.IsNullOrEmpty(assembly)) continue;
                CopyList[assembly + ".dll"] = Path.Combine(sourceDir, assembly + ".dll");
            }

            if (CopyList.Count == 0)
            {
                Debug.LogWarning("请检测 Setting 面板是否正确指定了热更 dll");
                return;
            }

            Copy(targetDir, (str) => { Debug.LogError($"{str} 不存在，请执行 CompileDll 后重试"); }, (str) => { Debug.Log($"成功复制热更Dll: {str}"); });
        }

        private static void Copy(string targetDir, Action<string> noExistsAction, Action<string> successAction = null)
        {
            if (!Directory.Exists(targetDir))
                Directory.CreateDirectory(targetDir);

            foreach (var copy in CopyList)
            {
                if (File.Exists(copy.Value))
                {
                    try
                    {
                        var destFileName = Path.Combine(targetDir, copy.Key + HybridCLRSettings.Instance.copyDllsExtension);
                        if (File.Exists(destFileName)) File.Delete(destFileName);
                        File.Copy(copy.Value, destFileName);
                        successAction?.Invoke(copy.Key);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"在复制{copy.Key}文件时发生异常，请重试 \n {e.Message}");
                        throw;
                    }
                }
                else
                {
                    noExistsAction?.Invoke(copy.Key);
                }
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }
    }
}