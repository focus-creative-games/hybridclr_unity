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
using System.Collections.Generic;
using UnityEngine;

namespace HybridCLR.CodeGen.MethodBridge
{

    public class CalliAnalyzer
    {
        private readonly List<ModuleDefMD> _rootModules = new List<ModuleDefMD>();

        private readonly List<CallNativeMethodSignatureInfo> _calliMethodSignatures = new List<CallNativeMethodSignatureInfo>();

        public List<CallNativeMethodSignatureInfo> CalliMethodSignatures => _calliMethodSignatures;

        public CalliAnalyzer(AssemblyCache cache, List<string> assemblyNames)
        {
            foreach (var assemblyName in assemblyNames)
            {
                _rootModules.Add(cache.LoadModule(assemblyName));
            }
        }

        private void CollectCalli()
        {
            foreach (var mod in _rootModules)
            {
                Debug.Log($"ass:{mod.FullName} methodcount:{mod.Metadata.TablesStream.MethodTable.Rows}");
                for (uint rid = 1, n = mod.Metadata.TablesStream.MethodTable.Rows; rid <= n; rid++)
                {
                    var method = mod.ResolveMethod(rid);
                    //Debug.Log($"method:{method}");
                    if (!method.HasBody)
                    {
                        continue;
                    }

                    foreach (var il in method.Body.Instructions)
                    {
                        if (il.OpCode.Code == dnlib.DotNet.Emit.Code.Calli)
                        {
                            MethodSig methodSig = (MethodSig)il.Operand;

                            _calliMethodSignatures.Add(new CallNativeMethodSignatureInfo()
                            {
                                MethodSig = methodSig,
                            });
                            Debug.Log($"method:{method}  calli method signature:{methodSig}");
                        }
                    }
                }
            }
        }

        public void Run()
        {
            CollectCalli();
        }
    }
}
