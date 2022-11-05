using dnlib.DotNet;
using HybridCLR.Editor.Commands;
using HybridCLR.Editor.Meta;
using HybridCLR.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace HybridCLR.Editor.DHE
{
    public class AssemblyOptionDataGenerator
    {
        public class Options
        {
            public string OutputDir { get; set; }

            public List<string> DifferentialHybridAssembyList { get; set; }
        }

        private readonly Options _options;

        public AssemblyOptionDataGenerator(Options options)
        {
            _options = options;
        }

        public void Generate()
        {
            foreach(string assName in _options.DifferentialHybridAssembyList)
            {
                using (var assCollector = new AssemblyCache(MetaUtil.CreateBuildTargetAssemblyResolver(EditorUserBuildSettings.activeBuildTarget)))
                {
                    var ass = assCollector.LoadModule(assName);

                    var dhaOptions = new DifferentialHybridAssemblyOptions()
                    {
                        notChangeMethodTokens = new List<uint>(),
                    };

                    foreach (var type in ass.GetTypes())
                    {
                        foreach (var method in type.Methods)
                        {
                            var attr = method.CustomAttributes.Where(a => a.AttributeType.FullName == "HybridCLR.Runtime.UnchangedAttribute").FirstOrDefault();
                            if (attr != null)
                            {
                                if ((bool)attr.ConstructorArguments[0].Value)
                                {
                                    dhaOptions.notChangeMethodTokens.Add(method.MDToken.Raw);
                                    Debug.Log($"Unchanged method:{method.MDToken.Raw}  {method}");
                                }
                            }
                        }
                    }
                    string outOptionFile = $"{_options.OutputDir}/{assName}.dhao.bytes";
                    File.WriteAllBytes(outOptionFile, dhaOptions.Marshal());
                    Debug.Log($"[AssemblyOptionDataGenerator] assembly:{ass} output:{outOptionFile}");
                }
            }
        }
    }
}
