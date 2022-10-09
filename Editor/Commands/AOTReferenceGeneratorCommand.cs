using HybridCLR.Editor.AOT;
using HybridCLR.Editor.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace HybridCLR.Editor.Commands
{
    public static class AOTReferenceGeneratorCommand
    {

        [MenuItem("HybridCLR/Generate/AOTGenericReference", priority = 102)]
        public static void GenerateAOTGenericReference()
        {
            GenerateAOTGenericReference(true);
        }

        public static void GenerateAOTGenericReference(bool compileDll)
        {
            // 此处理论会有点问题，打每个平台的时候，都得针对当前平台生成桥接函数
            // 但影响不大，先这样吧
            if (compileDll)
            {
                CompileDllCommand.CompileDllActiveBuildTarget();
            }

            var gs = SettingsUtil.HybridCLRSettings;

            using (AssemblyReferenceDeepCollector collector = new AssemblyReferenceDeepCollector(MetaUtil.CreateBuildTargetAssemblyResolver(EditorUserBuildSettings.activeBuildTarget), SettingsUtil.HotUpdateAssemblyNames))
            {
                var analyzer = new Analyzer(new Analyzer.Options
                {
                    MaxIterationCount = Math.Min(20, gs.maxGenericReferenceIteration),
                    Collector = collector,
                });

                analyzer.Run();

                var writer = new GenericReferenceWriter();
                writer.Write(analyzer.AotGenericTypes.ToList(), analyzer.AotGenericMethods.ToList(), $"{Application.dataPath}/{gs.outputAOTGenericReferenceFile}");
                AssetDatabase.Refresh();
            }
        }
    }
}
