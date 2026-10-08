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

﻿using HybridCLR.CppCodeGen;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace HybridCLR.CodeGen.Il2CppDef
{
    public class Il2CppDefGenerator
    {
        public class Options
        {
            public List<string> HotUpdateAssemblies { get; set; }

            public string UnityVersionOutputFile { get; set; }

            public string AssemblyManifestOutputFile { get; set; }

            public string UnityVersion { get; set; }
        }

        private readonly Options _options;
        public Il2CppDefGenerator(Options options)
        {
            _options = options;
        }


        private static readonly Regex s_unityVersionPat = new Regex(@"(\d+)\.(\d+)\.(\d+)");

        public void Generate()
        {
            GenerateUnityVersionInc();
            GenerateAssemblyManifestInc();
        }

        private void GenerateUnityVersionInc()
        {
            var cw = new CodeWriter();

            var match = s_unityVersionPat.Matches(_options.UnityVersion)[0];
            int majorVer = int.Parse(match.Groups[1].Value);
            int minorVer1 = int.Parse(match.Groups[2].Value);
            int minorVer2 = int.Parse(match.Groups[3].Value);

            cw.WriteLine($"#define HYBRIDCLR_UNITY_VERSION {majorVer}{minorVer1.ToString("D2")}{minorVer2.ToString("D2")}");

#if TUANJIE_1_1_OR_NEWER
            var tuanjieMatch = Regex.Matches(Application.tuanjieVersion, @"(\d+)\.(\d+)\.(\d+)");
            int tuanjieMajorVer = int.Parse(tuanjieMatch[0].Groups[1].Value);
            int tuanjieMinorVer1 = int.Parse(tuanjieMatch[0].Groups[2].Value);
            int tuanjieMinorVer2 = int.Parse(tuanjieMatch[0].Groups[3].Value);
            cw.WriteLine($"#define HYBRIDCLR_TUANJIE_VERSION {tuanjieMajorVer}{tuanjieMinorVer1.ToString("D2")}{tuanjieMinorVer2.ToString("D2")}");
#elif TUANJIE_2022_3_OR_NEWER
            cw.WriteLine($"#define HYBRIDCLR_TUANJIE_VERSION 10000");
#endif

            Directory.CreateDirectory(Path.GetDirectoryName(_options.UnityVersionOutputFile));
            cw.Save(_options.UnityVersionOutputFile);
            Debug.Log($"[HybridCLR.Il2CppDef.Generator] output:{_options.UnityVersionOutputFile}");
        }

        private void GenerateAssemblyManifestInc()
        {
            var cw = new CodeWriter();
            // Included inside g_placeHolderAssemblies initializer (4-space indent).
            cw.IncreaseIndent();

            foreach (var ass in _options.HotUpdateAssemblies)
            {
                cw.WriteLine($"\"{ass}\",");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(_options.AssemblyManifestOutputFile));
            cw.Save(_options.AssemblyManifestOutputFile);
            Debug.Log($"[HybridCLR.Il2CppDef.Generator] output:{_options.AssemblyManifestOutputFile}");
        }
    }
}
