﻿using HybridCLR.Editor.Link;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace HybridCLR.Editor.Commands
{

    public static class LinkGeneratorCommand
    {

        [MenuItem("HybridCLR/Generate/LinkXml", priority = 100)]
        public static void GenerateLinkXml()
        {
            GenerateLinkXml(true);
        }

        public static void GenerateLinkXml(bool compileDll)
        {
            if (compileDll)
            {
                CompileDllCommand.CompileDllActiveBuildTarget();
            }

            var ls = SettingsUtil.HybridCLRSettings;

            var allAssByNames = new Dictionary<string, Assembly>();
            foreach (var ass in AppDomain.CurrentDomain.GetAssemblies())
            {
                allAssByNames[ass.GetName().Name] = ass;
            }

            var hotfixAssembles = new List<Assembly>();
            foreach(var assName in SettingsUtil.HotUpdateAssemblyNames)
            {
                if (allAssByNames.TryGetValue(assName, out var ass))
                {
                    hotfixAssembles.Add(ass);
                }
                else
                {
                    throw new Exception($"assembly:{assName} 不存在");
                }
            }

            var analyzer = new Analyzer(Meta.MetaUtil.CreateBuildTargetAssemblyResolver(EditorUserBuildSettings.activeBuildTarget), HybridCLRSettings.Instance.collectAssetReferenceTypes);
            var refTypes = analyzer.CollectRefs(hotfixAssembles);

            Debug.Log($"[LinkGeneratorCommand] hotfix assembly count:{hotfixAssembles.Count}, ref type count:{refTypes.Count} output:{Application.dataPath}/{ls.outputLinkFile}");
            var linkXmlWriter = new LinkXmlWriter();
            linkXmlWriter.Write($"{Application.dataPath}/{ls.outputLinkFile}", refTypes);
            AssetDatabase.Refresh();
        }
    }
}
