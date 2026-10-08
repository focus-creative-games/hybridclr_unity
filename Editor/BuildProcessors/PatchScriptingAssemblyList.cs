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
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HybridCLR.BuildProcessors
{
    public class PatchScriptingAssemblyList :
#if UNITY_ANDROID
        IPostGenerateGradleAndroidProject,
#elif UNITY_OPENHARMONY
        UnityEditor.OpenHarmony.IPostGenerateOpenHarmonyProject,
#endif
        IPostprocessBuildWithReport

#if UNITY_PS5
        , IUnityLinkerProcessor
#endif

    {
        public int callbackOrder => 0;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            // When building apk directly there is no chance to patch ScriptingAssemblies.json in PostprocessBuild.
            // Therefore patch it at this stage
            // Unity bug: sometimes an apk path is passed in and replacement fails
            if (Directory.Exists(path))
            {
                PathScriptingAssembilesFile(path);
            }
            else
            {
                PathScriptingAssembilesFile($"{SettingsUtil.ProjectDir}/Library");
            }
        }

#if UNITY_OPENHARMONY

        public void OnPostGenerateOpenHarmonyProject(string path)
        {
            OnPostGenerateGradleAndroidProject(path);
        }

#endif

        public void OnPostprocessBuild(BuildReport report)
        {
            // For Android targets this was already handled in OnPostGenerateGradleAndroidProject,
            // so skip duplicate processing here
#if !UNITY_ANDROID && !UNITY_WEBGL && !UNITY_OPENHARMONY
            PathScriptingAssembilesFile(report.summary.outputPath);
#endif
        }

#if UNITY_PS5
        /// <summary>
        /// For Package build mode, patch .json at this stage; PC Hosted and GP5 modes are unaffected
        /// </summary>

        public string GenerateAdditionalLinkXmlFile(UnityEditor.Build.Reporting.BuildReport report, UnityEditor.UnityLinker.UnityLinkerBuildPipelineData data)
        {
            string path = $"{SettingsUtil.ProjectDir}/Library/PlayerDataCache/PS5/Data"; 
            PathScriptingAssembilesFile(path);
            return null;
        }
#endif
        public void PathScriptingAssembilesFile(string path)
        {
            if (!SettingsUtil.Enable)
            {
                Debug.Log($"[PatchScriptingAssemblyList] disabled");
                return;
            }
            Debug.Log($"[PatchScriptingAssemblyList]. path:{path}");
            if (!Directory.Exists(path))
            {
                path = Path.GetDirectoryName(path);
                Debug.Log($"[PatchScriptingAssemblyList] get path parent:{path}");
            }
            AddHotFixAssembliesToScriptingAssembliesJson(path);
        }

        private const string scriptingAssembliesJsonFile = "ScriptingAssemblies.json";

        private void AddHotFixAssembliesToScriptingAssembliesJson(string path)
        {
            Debug.Log($"[PatchScriptingAssemblyList]. path:{path}");
            /*
             * ScriptingAssemblies.json lists all dll names that are loaded automatically at startup;
             * dlls missing from this list cannot resolve types during asset deserialization
             * therefore entries removed by OnFilterAssemblies must be added back
             */
            string[] jsonFiles = Directory.GetFiles(path, scriptingAssembliesJsonFile, SearchOption.AllDirectories);

            if (jsonFiles.Length == 0)
            {
                Debug.LogWarning($"can not find file {scriptingAssembliesJsonFile}");
                return;
            }

            foreach (string file in jsonFiles)
            {
                var patcher = new ScriptingAssembliesJsonPatcher();
                patcher.Load(file);
                patcher.AddScriptingAssemblies(SettingsUtil.HotUpdateAssemblyFilesIncludePreserved);
                patcher.Save(file);
            }
        }
    }
}
