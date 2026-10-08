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

﻿using HybridCLR.Settings;
using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HybridCLR.BuildProcessors
{
    internal class CheckSettings : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public static bool DisableMethodBridgeDevelopmentFlagChecking { get; set; }

        public void OnPreprocessBuild(BuildReport report)
        {
            HybridCLRSettings globalSettings = SettingsUtil.HybridCLRSettings;
            if (!globalSettings.enable || globalSettings.useGlobalIl2cpp)
            {
                string oldIl2cppPath = Environment.GetEnvironmentVariable("UNITY_IL2CPP_PATH");
                if (!string.IsNullOrEmpty(oldIl2cppPath))
                {
                    Environment.SetEnvironmentVariable("UNITY_IL2CPP_PATH", "");
                    Debug.Log($"[CheckSettings] clean process environment variable: UNITY_IL2CPP_PATH, old vlaue:'{oldIl2cppPath}'");
                }
            }
            else
            {
                string curIl2cppPath = Environment.GetEnvironmentVariable("UNITY_IL2CPP_PATH");
                if (curIl2cppPath != SettingsUtil.LocalIl2CppDir)
                {
                    Environment.SetEnvironmentVariable("UNITY_IL2CPP_PATH", SettingsUtil.LocalIl2CppDir);
                    Debug.Log($"[CheckSettings] UNITY_IL2CPP_PATH old value:'{curIl2cppPath}'， new value:'{SettingsUtil.LocalIl2CppDir}'");
                }
            }
            if (!globalSettings.enable)
            {
                return;
            }
            BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
#if UNITY_6000_0_OR_NEWER
            NamedBuildTarget namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
            ScriptingImplementation curScriptingImplementation = PlayerSettings.GetScriptingBackend(namedBuildTarget);
#else
            ScriptingImplementation curScriptingImplementation = PlayerSettings.GetScriptingBackend(buildTargetGroup);
#endif
            ScriptingImplementation targetScriptingImplementation = ScriptingImplementation.IL2CPP;
            if (curScriptingImplementation != targetScriptingImplementation)
            {
                Debug.LogError($"[CheckSettings] current ScriptingBackend:{curScriptingImplementation}，have been switched to:{targetScriptingImplementation} automatically");
#if UNITY_6000_0_OR_NEWER
                PlayerSettings.SetScriptingBackend(namedBuildTarget, targetScriptingImplementation);
#else
                PlayerSettings.SetScriptingBackend(buildTargetGroup, targetScriptingImplementation);
#endif
            }

            var installer = new Installer.InstallerController();
            if (!installer.HasInstalledHybridCLR())
            {
                throw new BuildFailedException($"You have not initialized HybridCLR, please install it via menu 'HybridCLR/Installer'");
            }

            if (installer.PackageVersion != installer.InstalledLibil2cppVersion)
            {
                throw new BuildFailedException($"You must run `HybridCLR/Installer` after upgrading package");
            }

            HybridCLRSettings gs = SettingsUtil.HybridCLRSettings;
            if (((gs.hotUpdateAssemblies?.Length + gs.hotUpdateAssemblyDefinitions?.Length) ?? 0) == 0)
            {
                Debug.LogWarning("[CheckSettings] No hot update modules configured in HybridCLRSettings");
            }

            if (!DisableMethodBridgeDevelopmentFlagChecking)
            {
                string methodBridgeFile = $"{SettingsUtil.GeneratedCppDir}/MethodBridge.inc";
                var match = Regex.Match(File.ReadAllText(methodBridgeFile), @"// DEVELOPMENT=(\d)");
                if (match.Success)
                {
                    int developmentFlagInMethodBridge = int.Parse(match.Groups[1].Value);
                    int developmentFlagInEditorSettings = EditorUserBuildSettings.development ? 1 : 0;
                    if (developmentFlagInMethodBridge != developmentFlagInEditorSettings)
                    {
                        Debug.LogError($"[CheckSettings] MethodBridge.inc DEVELOPMENT flag:{developmentFlagInMethodBridge} is inconsistent with EditorUserBuildSettings.development:{developmentFlagInEditorSettings}. Please run 'HybridCLR/Generate/All' before building.");
                    }
                }
                else
                {
                    Debug.LogError("[CheckSettings] MethodBridge.inc DEVELOPMENT flag not found. Please run 'HybridCLR/Generate/All' before building.");
                }
            }
        }
    }
}
