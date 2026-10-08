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

﻿
using MonoHook;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace HybridCLR.BuildProcessors.Hooks
{
#if UNITY_2022 || UNITY_2023_1_OR_NEWER
    [InitializeOnLoad]
    public class GetIl2CppFolderHook
    {
        private static MethodHook _hook;

        static GetIl2CppFolderHook()
        {
            if (_hook == null)
            {
                Type type = typeof(UnityEditor.EditorApplication).Assembly.GetType("UnityEditorInternal.IL2CPPUtils");
                MethodInfo miTarget = type.GetMethod("GetIl2CppFolder", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public, null,
                    new Type[] { typeof(bool).MakeByRefType() }, null);

                MethodInfo miReplacement = new StripAssembliesDel(OverrideMethod).Method;
                MethodInfo miProxy = new StripAssembliesDel(PlaceHolderMethod).Method;

                _hook = new MethodHook(miTarget, miReplacement, miProxy);
                _hook.Install();
            }
        }

        private delegate string StripAssembliesDel(out bool isDevelopmentLocation);

        private static string OverrideMethod(out bool isDevelopmentLocation)
        {
            //Debug.Log("[GetIl2CppFolderHook] OverrideMethod");
            string result = PlaceHolderMethod(out isDevelopmentLocation);
            isDevelopmentLocation = false;
            return result;
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        private static string PlaceHolderMethod(out bool isDevelopmentLocation)
        {
            Debug.LogError("[GetIl2CppFolderHook] PlaceHolderMethod");
            isDevelopmentLocation = false;
            return null;
        }
    }
#endif
}
