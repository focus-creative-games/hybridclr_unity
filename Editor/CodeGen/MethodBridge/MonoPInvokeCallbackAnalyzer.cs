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

﻿using dnlib.DotNet;
using HybridCLR.Mod;
using HybridCLR.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HybridCLR.CodeGen.MethodBridge
{
    public class RawMonoPInvokeCallbackMethodInfo
    {
        public MethodDef Method { get; set; }

        public CustomAttribute GenerationAttribute { get; set; }
    }

    public class MonoPInvokeCallbackAnalyzer
    {

        private readonly List<ModuleDefMD> _rootModules = new List<ModuleDefMD>();

        private readonly List<RawMonoPInvokeCallbackMethodInfo> _reversePInvokeMethods = new List<RawMonoPInvokeCallbackMethodInfo>();

        public List<RawMonoPInvokeCallbackMethodInfo> ReversePInvokeMethods => _reversePInvokeMethods;

        public MonoPInvokeCallbackAnalyzer(AssemblyCache cache, List<string> assemblyNames)
        {
            foreach (var assemblyName in assemblyNames)
            {
                _rootModules.Add(cache.LoadModule(assemblyName));
            }
        }

        private void CollectReversePInvokeMethods()
        {
            foreach (var mod in _rootModules)
            {
                Debug.Log($"ass:{mod.FullName} method count:{mod.Metadata.TablesStream.MethodTable.Rows}");
                for (uint rid = 1, n = mod.Metadata.TablesStream.MethodTable.Rows; rid <= n; rid++)
                {
                    var method = mod.ResolveMethod(rid);
                    //Debug.Log($"method:{method}");
                    if (!method.IsStatic || !method.HasCustomAttributes)
                    {
                        continue;
                    }
                    CustomAttribute wa = method.CustomAttributes.FirstOrDefault(ca => ca.AttributeType.Name == "MonoPInvokeCallbackAttribute");
                    if (wa == null)
                    {
                        continue;
                    }
                    if (!MetaUtil.IsSupportedPInvokeMethodSignature(method.MethodSig))
                    {
                        Debug.LogError($"MonoPInvokeCallback method {method.FullName} has unsupported parameter or return type. Please check the method signature.");
                    }
                    //foreach (var ca in method.CustomAttributes)
                    //{
                    //    Debug.Log($"{ca.AttributeType.FullName} {ca.TypeFullName}");
                    //}
                    _reversePInvokeMethods.Add(new RawMonoPInvokeCallbackMethodInfo()
                    {
                        Method = method,
                        GenerationAttribute = method.CustomAttributes.FirstOrDefault(ca => ca.AttributeType.FullName == "HybridCLR.ReversePInvokeWrapperGenerationAttribute"),
                    });
                }
            }
        }

        public void Run()
        {
            CollectReversePInvokeMethods();
        }
    }
}
