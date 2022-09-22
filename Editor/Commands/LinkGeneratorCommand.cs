using HybridCLR.Editor.LinkGenerator;
using HybridCLR.Editor.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace HybridCLR.Editor.Commands
{

    public static class LinkGeneratorCommand
    {

        [MenuItem("HybridCLR/GenerateLinkXml", priority = 10)]
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

            var ls = SettingsUtil.GlobalSettings;

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

            var analyzer = new Analyzer(MetaUtil.CreateBuildTargetAssemblyResolver(EditorUserBuildSettings.activeBuildTarget));
            var refTypes = analyzer.CollectRefs(hotfixAssembles);

            Debug.Log($"[LinkGeneratorCommand] hotfix assembly count:{hotfixAssembles.Count}, ref type count:{refTypes.Count} output:{Application.dataPath}/{ls.outputLinkFile}");
            var linkXmlWriter = new LinkXmlWriter();
            linkXmlWriter.Write($"{Application.dataPath}/{ls.outputLinkFile}", refTypes);
        }
    }
}
