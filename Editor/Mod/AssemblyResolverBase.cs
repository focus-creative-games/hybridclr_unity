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

﻿using System;

namespace HybridCLR.Mod
{
    public abstract class AssemblyResolverBase : IAssemblyResolver
    {
        public string ResolveAssembly(string assemblyName, bool throwExIfNotFind)
        {
            if (TryResolveAssembly(assemblyName, out string assemblyPath))
            {
                return assemblyPath;
            }
            if (throwExIfNotFind)
            {
                if (SettingsUtil.HotUpdateAssemblyNamesIncludePreserved.Contains(assemblyName))
                {
                    throw new Exception($"resolve Hot update dll:{assemblyName} failed! Please make sure that this hot update dll exists or the search path is configured in the external hot update path.");
                }
                else
                {
                    throw new Exception($"resolve AOT dll:{assemblyName} failed! Please make sure that the AOT project has referenced the dll and generated the trimmed AOT dll correctly.");
                }
            }
            return null;
        }

        protected abstract bool TryResolveAssembly(string assemblyName, out string assemblyPath);
    }
}
