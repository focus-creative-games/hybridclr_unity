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
using HybridCLR.CodeGen.Link;
using HybridCLR.Utils;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HybridCLR.Commands
{
    public static class LinkGeneratorCommand
    {

        [MenuItem("HybridCLR/Generate/LinkXml", priority = 100)]
        public static void GenerateLinkXml()
        {
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            CompileDllCommand.CompileDll(target);
            GenerateLinkXml(target);
        }

        public static void GenerateLinkXml(BuildTarget target)
        {
            var ls = SettingsUtil.HybridCLRSettings;

            List<string> hotfixAssemblies = SettingsUtil.HotUpdateAssemblyNamesExcludePreserved;

            var analyzer = new LinkAnalyzer(MetaUtil.CreateHotUpdateAndAOTAssemblyResolver(target, hotfixAssemblies));
            var preserveTypes = analyzer.CollectRefs(hotfixAssemblies, ls.linkPreserveLevel);

            Debug.Log($"[LinkGeneratorCommand] hotfix assembly count:{hotfixAssemblies.Count}, preserve level:{ls.linkPreserveLevel}, type count:{preserveTypes.Count} output:{Application.dataPath}/{ls.outputLinkFile}");
            var linkXmlWriter = new LinkXmlWriter();
            linkXmlWriter.Write($"{Application.dataPath}/{ls.outputLinkFile}", preserveTypes);
            AssetDatabase.Refresh();
        }
    }
}
