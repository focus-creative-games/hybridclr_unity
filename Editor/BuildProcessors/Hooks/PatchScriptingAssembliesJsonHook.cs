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

﻿namespace HybridCLR.BuildProcessors.Hooks
{
#if UNITY_2021_1_OR_NEWER && (UNITY_WEBGL || UNITY_WEIXINMINIGAME)
    [InitializeOnLoad]
    public class PatchScriptingAssembliesJsonHook
    {
        private static MethodHook _hook;

        static PatchScriptingAssembliesJsonHook()
        {
            if (_hook == null)
            {
                Type type = typeof(UnityEditor.EditorApplication);
                MethodInfo miTarget = type.GetMethod("BuildMainWindowTitle", BindingFlags.Static | BindingFlags.NonPublic);

                MethodInfo miReplacement = new Func<string>(BuildMainWindowTitle).Method;
                MethodInfo miProxy = new Func<string>(BuildMainWindowTitleProxy).Method;

                _hook = new MethodHook(miTarget, miReplacement, miProxy);
                _hook.Install();
            }
        }

        private static string BuildMainWindowTitle()
        {
        var cacheDir = $"{Application.dataPath}/../Library/PlayerDataCache";
        if (Directory.Exists(cacheDir))
            {
                foreach (var tempJsonPath in Directory.GetDirectories(cacheDir, "*", SearchOption.TopDirectoryOnly))
                {
                    string dirName = Path.GetFileName(tempJsonPath);
#if UNITY_WEIXINMINIGAME
                    if (!dirName.Contains("WeixinMiniGame"))
                    {
                        continue;
                    }
#else
                    if (!dirName.Contains("WebGL"))
                    {
                        continue;
                    }
#endif

                    var patcher = new PatchScriptingAssemblyList();
                    patcher.PathScriptingAssembilesFile(tempJsonPath);
                }
            }

            string newTitle = BuildMainWindowTitleProxy();
            return newTitle;
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        private static string BuildMainWindowTitleProxy()
        {
            // Padding added only to satisfy MonoHook minimum code length requirements
            UnityEngine.Debug.Log(12345.ToString());
            return string.Empty;
        }
    }
#endif
}
