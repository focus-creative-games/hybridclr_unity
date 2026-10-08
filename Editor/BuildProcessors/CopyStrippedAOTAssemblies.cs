// Copyright 2026 Code Philosophy
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using HybridCLR.Utils;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
#if !UNITY_2021_1_OR_NEWER
using UnityEditor.Il2Cpp;
#endif

namespace HybridCLR.BuildProcessors
{
    internal class CopyStrippedAOTAssemblies : IPostprocessBuildWithReport, IPreprocessBuildWithReport
    {

        public int callbackOrder => 0;

#if UNITY_2021_1_OR_NEWER
        public static string GetStripAssembliesDir2021(BuildTarget target)
        {
            string projectDir = SettingsUtil.ProjectDir;
            switch (target)
            {
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
            return $"{projectDir}/Library/Bee/artifacts/WinPlayerBuildProgram/ManagedStripped";
            case BuildTarget.StandaloneLinux64:
            return $"{projectDir}/Library/Bee/artifacts/LinuxPlayerBuildProgram/ManagedStripped";
            case BuildTarget.WSAPlayer:
            return $"{projectDir}/Library/Bee/artifacts/UWPPlayerBuildProgram/ManagedStripped";
            case BuildTarget.Android:
            return $"{projectDir}/Library/Bee/artifacts/Android/ManagedStripped";
#if TUANJIE_2022_3_OR_NEWER
                case BuildTarget.HMIAndroid:
                    return $"{projectDir}/Library/Bee/artifacts/HMIAndroid/ManagedStripped";
#endif
            case BuildTarget.iOS:
#if UNITY_TVOS
                case BuildTarget.tvOS:
#endif
            return $"{projectDir}/Library/Bee/artifacts/iOS/ManagedStripped";
#if UNITY_VISIONOS
                case BuildTarget.VisionOS:
#if UNITY_6000_0_OR_NEWER
                return $"{projectDir}/Library/Bee/artifacts/VisionOS/ManagedStripped";
#else
                return $"{projectDir}/Library/Bee/artifacts/iOS/ManagedStripped";
#endif
#endif
            case BuildTarget.WebGL:
            return $"{projectDir}/Library/Bee/artifacts/WebGL/ManagedStripped";
            case BuildTarget.StandaloneOSX:
            return $"{projectDir}/Library/Bee/artifacts/MacStandalonePlayerBuildProgram/ManagedStripped";
            case BuildTarget.PS4:
            return $"{projectDir}/Library/Bee/artifacts/PS4PlayerBuildProgram/ManagedStripped";
            case BuildTarget.PS5:
            return $"{projectDir}/Library/Bee/artifacts/PS5PlayerBuildProgram/ManagedStripped";
            case BuildTarget.GameCoreXboxOne:
            case BuildTarget.GameCoreXboxSeries:
            return $"{projectDir}/Library/Bee/artifacts/GameCorePlayerBuildProgram/ManagedStripped";
#if UNITY_WEIXINMINIGAME
                case BuildTarget.WeixinMiniGame:
                    return $"{projectDir}/Library/Bee/artifacts/WeixinMiniGame/ManagedStripped";
#endif
#if UNITY_OPENHARMONY
                case BuildTarget.OpenHarmony:
                    return $"{projectDir}/Library/Bee/artifacts/OpenHarmonyPlayerBuildProgram/ManagedStripped";
#endif
            default: return "";
            }
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
            BuildTarget target = report.summary.platform;
            CopyStripDlls(GetStripAssembliesDir2020(target), target);
        }
#endif

        public static void CopyStripDlls(string srcStripDllPath, BuildTarget target)
        {
            if (!SettingsUtil.Enable)
            {
                Debug.Log($"[CopyStrippedAOTAssemblies] disabled");
                return;
            }
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
#if UNITY_2021_1_OR_NEWER
            BuildTarget target = report.summary.platform;
            string srcStripDllPath = GetStripAssembliesDir2021(target);
            if (!string.IsNullOrEmpty(srcStripDllPath) && Directory.Exists(srcStripDllPath))
            {
                CopyStripDlls(srcStripDllPath, target);
            }
#endif
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            BuildTarget target = report.summary.platform;
            var dstPath = SettingsUtil.GetAssembliesPostIl2CppStripDir(target);
            DirectoryUtil.RecreateDir(dstPath);
        }
    }
}
