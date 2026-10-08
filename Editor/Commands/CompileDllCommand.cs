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

﻿using System.IO;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEngine;

namespace HybridCLR.Commands
{
    public class CompileDllCommand
    {
        public static void CompileDll(string buildDir, BuildTarget target, bool developmentBuild)
        {
            var group = BuildPipeline.GetBuildTargetGroup(target);

            ScriptCompilationSettings scriptCompilationSettings = new ScriptCompilationSettings();
            scriptCompilationSettings.group = group;
            scriptCompilationSettings.target = target;
            scriptCompilationSettings.options = developmentBuild ? ScriptCompilationOptions.DevelopmentBuild : ScriptCompilationOptions.None;
            Directory.CreateDirectory(buildDir);
            ScriptCompilationResult scriptCompilationResult = PlayerBuildInterface.CompilePlayerScripts(scriptCompilationSettings, buildDir);
#if UNITY_2022
            UnityEditor.EditorUtility.ClearProgressBar();
#endif
            Debug.Log($"compile finish!!! buildDir:{buildDir} target:{target} development:{developmentBuild}");
        }

        public static void CompileDll(BuildTarget target)
        {
            CompileDll(target, EditorUserBuildSettings.development);
        }

        public static void CompileDll(BuildTarget target, bool developmentBuild)
        {
            CompileDll(SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target), target, developmentBuild);
        }

        [MenuItem("HybridCLR/CompileDll/ActiveBuildTarget", priority = 100)]
        public static void CompileDllActiveBuildTarget()
        {
            CompileDll(EditorUserBuildSettings.activeBuildTarget, EditorUserBuildSettings.development);
        }

        [MenuItem("HybridCLR/CompileDll/ActiveBuildTarget_Release", priority = 102)]
        public static void CompileDllActiveBuildTargetRelease()
        {
            CompileDll(EditorUserBuildSettings.activeBuildTarget, false);
        }

        [MenuItem("HybridCLR/CompileDll/ActiveBuildTarget_Development", priority = 104)]
        public static void CompileDllActiveBuildTargetDevelopment()
        {
            CompileDll(EditorUserBuildSettings.activeBuildTarget, true);
        }

        [MenuItem("HybridCLR/CompileDll/Win32", priority = 200)]
        public static void CompileDllWin32()
        {
            CompileDll(BuildTarget.StandaloneWindows);
        }

        [MenuItem("HybridCLR/CompileDll/Win64", priority = 201)]
        public static void CompileDllWin64()
        {
            CompileDll(BuildTarget.StandaloneWindows64);
        }

        [MenuItem("HybridCLR/CompileDll/MacOS", priority = 202)]
        public static void CompileDllMacOS()
        {
            CompileDll(BuildTarget.StandaloneOSX);
        }

        [MenuItem("HybridCLR/CompileDll/Linux", priority = 203)]
        public static void CompileDllLinux()
        {
            CompileDll(BuildTarget.StandaloneLinux64);
        }

        [MenuItem("HybridCLR/CompileDll/Android", priority = 210)]
        public static void CompileDllAndroid()
        {
            CompileDll(BuildTarget.Android);
        }

        [MenuItem("HybridCLR/CompileDll/IOS", priority = 220)]
        public static void CompileDllIOS()
        {
            CompileDll(BuildTarget.iOS);
        }

        [MenuItem("HybridCLR/CompileDll/WebGL", priority = 230)]
        public static void CompileDllWebGL()
        {
            CompileDll(BuildTarget.WebGL);
        }
    }
}
