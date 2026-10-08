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

﻿using HybridCLR.BuildProcessors;
using HybridCLR.Utils;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HybridCLR.Commands
{
    public static class StripAOTDllCommand
    {
        [MenuItem("HybridCLR/Generate/StrippedAotDlls", priority = 105)]
        public static void GenerateStripedAOTDlls()
        {
            GenerateStripedAOTDlls(EditorUserBuildSettings.activeBuildTarget);
        }

        static BuildOptions GetBuildPlayerOptions(BuildTarget buildTarget)
        {
            BuildOptions options = BuildOptions.None;
            bool development = EditorUserBuildSettings.development;
            if (development)
            {
                options |= BuildOptions.Development;
            }

            if (EditorUserBuildSettings.allowDebugging && development)
            {
                options |= BuildOptions.AllowDebugging;
            }

            if (EditorUserBuildSettings.connectProfiler && (development || buildTarget == BuildTarget.WSAPlayer))
            {
                options |= BuildOptions.ConnectWithProfiler;
            }

            if (EditorUserBuildSettings.buildWithDeepProfilingSupport && development)
            {
                options |= BuildOptions.EnableDeepProfilingSupport;
            }

#if UNITY_2021_2_OR_NEWER
            options |= BuildOptions.CleanBuildCache;
#endif

            return options;
        }

        private static string GetLocationPathName(string buildDir, BuildTarget target)
        {
            switch (target)
            {
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64: return $"{buildDir}/{PlayerSettings.productName}.exe";
            case BuildTarget.StandaloneOSX: return buildDir;
            case BuildTarget.iOS: return buildDir;
            case BuildTarget.Android: return buildDir;
            case BuildTarget.StandaloneLinux64: return $"{buildDir}/{PlayerSettings.productName}";
            default: return buildDir;
            }
        }

        public static void GenerateStripedAOTDlls(BuildTarget target)
        {
            string outputPath = $"Temp/HybridCLR/StrippedAOTDllsTempProj/{target}";
            DirectoryUtil.RemoveDir(outputPath);

            var buildOptions = GetBuildPlayerOptions(target);

            bool oldExportAndroidProj = EditorUserBuildSettings.exportAsGoogleAndroidProject;
#if UNITY_EDITOR_OSX
            bool oldCreateSolution = UnityEditor.OSXStandalone.UserBuildSettings.createXcodeProject;
#elif UNITY_EDITOR_WIN
            bool oldCreateSolution = UnityEditor.WindowsStandalone.UserBuildSettings.createSolution;
#endif
#if TUANJIE_2022_3_OR_NEWER
            bool oldOpenHarmonyProj = EditorUserBuildSettings.exportAsOpenHarmonyProject;
#endif
            bool oldBuildScriptsOnly = EditorUserBuildSettings.buildScriptsOnly;

            string oldBuildLocation = EditorUserBuildSettings.GetBuildLocation(target);
            try
            {
                CheckSettings.DisableMethodBridgeDevelopmentFlagChecking = true;
                EditorUserBuildSettings.buildScriptsOnly = true;

                string location = GetLocationPathName(outputPath, target);
                EditorUserBuildSettings.SetBuildLocation(target, location);

                switch (target)
                {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                {
#if UNITY_EDITOR_WIN
                    UnityEditor.WindowsStandalone.UserBuildSettings.createSolution = true;
#endif
                    break;
                }
                case BuildTarget.StandaloneOSX:
                {
#if UNITY_EDITOR_OSX
                        UnityEditor.OSXStandalone.UserBuildSettings.createXcodeProject = true;
#endif
                    break;
                }
#if TUANJIE_2022_3_OR_NEWER
                    case BuildTarget.HMIAndroid:
#endif
                case BuildTarget.Android:
                {
                    EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
                    break;
                }
#if TUANJIE_2022_3_OR_NEWER
                    case BuildTarget.OpenHarmony:
                    {
                        EditorUserBuildSettings.exportAsOpenHarmonyProject = true;
                        break;
                    }
#endif
                }

                Debug.Log($"GenerateStripedAOTDlls build option:{buildOptions}");

                BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions()
                {
                    scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                    locationPathName = location,
                    options = buildOptions,
                    target = target,
                    targetGroup = BuildPipeline.GetBuildTargetGroup(target),
#if UNITY_SERVER
                    subtarget = (int)StandaloneBuildSubtarget.Server,
#endif
                };

                var report = BuildPipeline.BuildPlayer(buildPlayerOptions);



                if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                {
                    throw new Exception("GenerateStripedAOTDlls failed");
                }
            }
            finally
            {
                CheckSettings.DisableMethodBridgeDevelopmentFlagChecking = false;
                EditorUserBuildSettings.buildScriptsOnly = oldBuildScriptsOnly;
                EditorUserBuildSettings.SetBuildLocation(target, oldBuildLocation);

                switch (target)
                {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                {
#if UNITY_EDITOR_WIN
                    UnityEditor.WindowsStandalone.UserBuildSettings.createSolution = oldCreateSolution;
#endif
                    break;
                }
                case BuildTarget.StandaloneOSX:
                {
#if UNITY_EDITOR_OSX
                            UnityEditor.OSXStandalone.UserBuildSettings.createXcodeProject = oldCreateSolution;
#endif
                    break;
                }
#if TUANJIE_2022_3_OR_NEWER
                    case BuildTarget.HMIAndroid:
#endif
                case BuildTarget.Android:
                {
                    EditorUserBuildSettings.exportAsGoogleAndroidProject = oldExportAndroidProj;
                    break;
                }
#if TUANJIE_2022_3_OR_NEWER
                    case BuildTarget.OpenHarmony:
                    {
                        EditorUserBuildSettings.exportAsOpenHarmonyProject = oldOpenHarmonyProj;
                        break;
                    }
#endif
                }
            }
            Debug.Log($"GenerateStripedAOTDlls target:{target} path:{outputPath}");
        }
    }
}
