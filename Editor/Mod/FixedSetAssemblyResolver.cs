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

﻿using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace HybridCLR.Mod
{
    public class FixedSetAssemblyResolver : AssemblyResolverBase
    {
        private readonly string _rootDir;
        private readonly HashSet<string> _fileNames;

        public FixedSetAssemblyResolver(string rootDir, IEnumerable<string> fileNameNotExts)
        {
            _rootDir = rootDir;
            _fileNames = new HashSet<string>(fileNameNotExts);
        }

        protected override bool TryResolveAssembly(string assemblyName, out string assemblyPath)
        {
            if (_fileNames.Contains(assemblyName))
            {
                assemblyPath = $"{_rootDir}/{assemblyName}.dll";
                if (File.Exists(assemblyPath))
                {
                    Debug.Log($"[FixedSetAssemblyResolver] resolve:{assemblyName} path:{assemblyPath}");
                    return true;
                }
                assemblyPath = $"{_rootDir}/{assemblyName}.dll.bytes";
                if (File.Exists(assemblyPath))
                {
                    Debug.Log($"[FixedSetAssemblyResolver] resolve:{assemblyName} path:{assemblyPath}");
                    return true;
                }
            }
            assemblyPath = null;
            return false;
        }
    }
}
