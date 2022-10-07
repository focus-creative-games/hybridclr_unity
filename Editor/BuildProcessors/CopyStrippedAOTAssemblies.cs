﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Il2Cpp;
using UnityEditor.UnityLinker;
using UnityEngine;

namespace HybridCLR.Editor.BuildProcessors
{
    internal class CopyStrippedAOTAssemblies : IPostprocessBuildWithReport
#if UNITY_ANDROID
    , IPostGenerateGradleAndroidProject
#endif
#if !UNITY_2021_1_OR_NEWER
     , IIl2CppProcessor
#endif
    {

        public int callbackOrder => 0;

#if UNITY_2021_1_OR_NEWER
        public static string GetStripAssembliesDir2021(BuildTarget target)
        {
            string projectDir = SettingsUtil.ProjectDir;
#if UNITY_STANDALONE_WIN
            return $"{projectDir}/Library/Bee/artifacts/WinPlayerBuildProgram/ManagedStripped";
#elif UNITY_ANDROID
            return $"{projectDir}/Library/Bee/artifacts/Android/ManagedStripped";
#elif UNITY_IOS
            return $"{projectDir}/Temp/StagingArea/Data/Managed/tempStrip";
#elif UNITY_WEBGL
            return $"{projectDir}/Library/Bee/artifacts/WebGL/ManagedStripped";
#elif UNITY_EDITOR_OSX
            return $"{projectDir}/Library/Bee/artifacts/MacStandalonePlayerBuildProgram/ManagedStripped";
#else
            throw new NotSupportedException("GetOriginBuildStripAssembliesDir");
#endif
        }
#else
        private string GetStripAssembliesDir2020(BuildTarget target)
        {
            string subPath = target == BuildTarget.Android ?
                "assets/bin/Data/Managed" :
                "Data/Managed/";
            return $"{SettingsUtil.ProjectDir}/Temp/StagingArea/{subPath}";
        }

        public void OnBeforeConvertRun(BuildReport report, Il2CppBuildPipelineData data)
        {            
            // 此回调只在 2020中调用
            CopyStripDlls(GetStripAssembliesDir2020(data.target), data.target);
        }
#endif

        public static void CopyStripDlls(string srcStripDllPath, BuildTarget target)
        {
            Debug.Log($"[CopyStrippedAOTAssemblies] CopyScripDlls. src:{srcStripDllPath} target:{target}");

            var dstPath = SettingsUtil.GetAssembliesPostIl2CppStripDir(target);

            Directory.CreateDirectory(dstPath);

            foreach (var fileFullPath in Directory.GetFiles(srcStripDllPath, "*.dll"))
            {
                var file = Path.GetFileName(fileFullPath);
                Debug.Log($"[CopyStrippedAOTAssemblies] copy strip dll {fileFullPath} ==> {dstPath}/{file}");
                File.Copy($"{fileFullPath}", $"{dstPath}/{file}", true);
            }
        }

        public void OnPostprocessBuild(BuildReport report)
        {
#if UNITY_2021_1_OR_NEWER && !UNITY_IOS
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            CopyStripDlls(GetStripAssembliesDir2021(target), target);
#endif
        }
        public void OnPostGenerateGradleAndroidProject(string path)
        {
#if UNITY_2021_1_OR_NEWER && !UNITY_IOS
            if (EditorUserBuildSettings.exportAsGoogleAndroidProject)
            {
                BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
                CopyStripDlls(GetStripAssembliesDir2021(target), target);
            }
#endif
        }
    }
}
