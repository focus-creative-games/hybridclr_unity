using HybridCLR.Editor.Link;
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

            List<string> hotfixAssemblies = SettingsUtil.HotUpdateAssemblyNames;

            var analyzer = new Analyzer(Meta.MetaUtil.CreateBuildTargetAssemblyResolver(EditorUserBuildSettings.activeBuildTarget), HybridCLRSettings.Instance.collectAssetReferenceTypes);
            var refTypes = analyzer.CollectRefs(hotfixAssemblies);

            Debug.Log($"[LinkGeneratorCommand] hotfix assembly count:{hotfixAssemblies.Count}, ref type count:{refTypes.Count} output:{Application.dataPath}/{ls.outputLinkFile}");
            var linkXmlWriter = new LinkXmlWriter();
            linkXmlWriter.Write($"{Application.dataPath}/{ls.outputLinkFile}", refTypes);
            AssetDatabase.Refresh();
        }
    }
}
