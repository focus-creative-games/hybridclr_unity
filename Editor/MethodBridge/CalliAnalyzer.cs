using dnlib.DotNet;
using HybridCLR.Editor.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace HybridCLR.Editor.MethodBridge
{

    public class RawCalliMethodSignatureInfo
    {
        public MethodSig MethodSig { get; set; }
    }

    public class CalliAnalyzer
    {
        private readonly List<ModuleDefMD> _rootModules = new List<ModuleDefMD>();

        private readonly List<RawCalliMethodSignatureInfo> _calliMethodSignatures = new List<RawCalliMethodSignatureInfo>();

        public List<RawCalliMethodSignatureInfo> CalliMethodSignatures => _calliMethodSignatures;

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

                            _calliMethodSignatures.Add(new RawCalliMethodSignatureInfo()
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
