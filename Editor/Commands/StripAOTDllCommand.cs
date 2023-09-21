﻿using HybridCLR.Editor.Installer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using static UnityEngine.Networking.UnityWebRequest;

namespace HybridCLR.Editor.Commands
{
    public static class StripAOTDllCommand
    {
        [MenuItem("HybridCLR/Generate/AOTDlls", priority = 105)]
        public static void GenerateStripedAOTDlls()
        {
            GenerateStripedAOTDlls(EditorUserBuildSettings.activeBuildTarget);
        }

        static BuildOptions GetBuildPlayerOptions(BuildTarget buildTarget)
        {
            BuildOptions options = BuildOptions.BuildScriptsOnly;
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
            switch(target)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64: return $"{buildDir}/{target}";
                case BuildTarget.StandaloneOSX: return buildDir;
                case BuildTarget.iOS: return buildDir;
                case BuildTarget.Android: return buildDir;
                case BuildTarget.StandaloneLinux64: return buildDir;
                default: return buildDir;
            }
        }

        public static void GenerateStripedAOTDlls(BuildTarget target)
        {
            string outputPath = $"{SettingsUtil.HybridCLRDataDir}/StrippedAOTDllsTempProj/{target}";
            BashUtil.RemoveDir(outputPath);

            var buildOptions = GetBuildPlayerOptions(target);
#if UNITY_2021_2_OR_NEWER
            buildOptions |= BuildOptions.CleanBuildCache;
#endif

            bool oldExportAndroidProj = EditorUserBuildSettings.exportAsGoogleAndroidProject;
#if UNITY_EDITOR_OSX
            bool oldCreateSolution = UnityEditor.OSXStandalone.UserBuildSettings.createXcodeProject;
#elif UNITY_EDITOR_WIN
            bool oldCreateSolution = UnityEditor.WindowsStandalone.UserBuildSettings.createSolution;
#endif
            bool oldBuildScriptsOnly = EditorUserBuildSettings.buildScriptsOnly;
            EditorUserBuildSettings.buildScriptsOnly = true;

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
                case BuildTarget.Android:
                {
                    EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
                    break;
                }
            }

            Debug.Log($"GenerateStripedAOTDlls build option:{buildOptions}");

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions()
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = GetLocationPathName(outputPath, target),
                options = buildOptions,
                target = target,
                targetGroup = BuildPipeline.GetBuildTargetGroup(target),
            };

            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);

            EditorUserBuildSettings.buildScriptsOnly = oldBuildScriptsOnly;
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
                case BuildTarget.Android:
                {
                    EditorUserBuildSettings.exportAsGoogleAndroidProject = oldExportAndroidProj;
                    break;
                }
            }

            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                throw new Exception("GenerateStripedAOTDlls failed");
            }
            Debug.Log($"GenerateStripedAOTDlls target:{target} path:{outputPath}");
        }
    }
}
