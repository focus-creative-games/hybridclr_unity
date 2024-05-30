using dnlib.DotNet;
using HybridCLR.Editor.ABI;
using HybridCLR.Editor.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CallingConvention = System.Runtime.InteropServices.CallingConvention;
using UnityEngine;

namespace HybridCLR.Editor.ReversePInvokeWrap
{
    public class RawReversePInvokeMethodInfo
    {
        public MethodDef Method { get; set; }

        public CustomAttribute GenerationAttribute { get; set; }
    }

    public class ABIReversePInvokeMethodInfo
    {
        public MethodDesc Method { get; set; }

        public CallingConvention Callvention { get; set; }

        public int Count { get; set; }

        public string Signature { get; set; }
    }

    public class Analyzer
    {

        private readonly List<ModuleDefMD> _rootModules = new List<ModuleDefMD>();

        private readonly List<RawReversePInvokeMethodInfo> _reversePInvokeMethods = new List<RawReversePInvokeMethodInfo>();

        public Analyzer(AssemblyCache cache, List<string> assemblyNames)
        {
            foreach (var assemblyName in assemblyNames)
            {
                _rootModules.Add(cache.LoadModule(assemblyName));
            }
        }

        private void CollectReversePInvokeMethods()
        {
            foreach (var mod in _rootModules)
            {
                Debug.Log($"ass:{mod.FullName} methodcount:{mod.Metadata.TablesStream.MethodTable.Rows}");
                for (uint rid = 1, n = mod.Metadata.TablesStream.MethodTable.Rows; rid <= n; rid++)
                {
                    var method = mod.ResolveMethod(rid);
                    //Debug.Log($"method:{method}");
                    if (!method.IsStatic || !method.HasCustomAttributes)
                    {
                        continue;
                    }
                    CustomAttribute wa = method.CustomAttributes.FirstOrDefault(ca => ca.AttributeType.Name == "MonoPInvokeCallbackAttribute");
                    if (wa == null)
                    {
                        continue;
                    }
                    //foreach (var ca in method.CustomAttributes)
                    //{
                    //    Debug.Log($"{ca.AttributeType.FullName} {ca.TypeFullName}");
                    //}
                    _reversePInvokeMethods.Add(new RawReversePInvokeMethodInfo()
                    {
                        Method = method,
                        GenerationAttribute = method.CustomAttributes.FirstOrDefault(ca => ca.AttributeType.FullName == "HybridCLR.ReversePInvokeWrapperGenerationAttribute"),
                    });
                }
            }
        }

        private static string MakeSignature(MethodDesc desc, CallingConvention CallingConventionention)
        {
            string convStr = ((char)('A' + (int)CallingConventionention - 1)).ToString();
            return $"{convStr}{desc.Sig}";
        }

        private static CallingConvention GetCallingConvention(MethodDef method)
        {
            var monoPInvokeCallbackAttr = method.CustomAttributes.FirstOrDefault(ca => ca.AttributeType.Name == "MonoPInvokeCallbackAttribute");
            if (monoPInvokeCallbackAttr == null)
            {
                return CallingConvention.Winapi;
            }
            object delegateTypeSig = monoPInvokeCallbackAttr.ConstructorArguments[0].Value;

            TypeDef delegateTypeDef;
            if (delegateTypeSig is ClassSig classSig)
            {
                delegateTypeDef = classSig.TypeDefOrRef.ResolveTypeDefThrow();
            }
            else if (delegateTypeSig is GenericInstSig genericInstSig)
            {
                delegateTypeDef = genericInstSig.GenericType.TypeDefOrRef.ResolveTypeDefThrow();
            }
            else
            {
                delegateTypeDef = null;
            }

            if (delegateTypeDef == null)
            {
                throw new NotSupportedException($"Unsupported delegate type {delegateTypeSig.GetType()}");
            }
            var attr = delegateTypeDef.CustomAttributes.FirstOrDefault(ca => ca.AttributeType.FullName == "System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute");
            if (attr == null)
            {
                return CallingConvention.Winapi;
            }
            var conv = attr.ConstructorArguments[0].Value;
            return (CallingConvention)conv;
        }

        public List<ABIReversePInvokeMethodInfo> BuildABIMethods()
        {
            var methodsBySig = new Dictionary<string, ABIReversePInvokeMethodInfo>();
            var typeCreator = new TypeCreator();
            foreach(var method in _reversePInvokeMethods)
            {
                MethodDesc desc = new MethodDesc
                {
                    MethodDef = method.Method,
                    ReturnInfo = new ReturnInfo { Type = typeCreator.CreateTypeInfo(method.Method.ReturnType)},
                    ParamInfos = method.Method.Parameters.Select(p => new ParamInfo { Type = typeCreator.CreateTypeInfo(p.Type)}).ToList(),
                };
                desc.Init();

                CallingConvention callingConv = GetCallingConvention(method.Method);
                string signature = MakeSignature(desc, callingConv);

                if (!methodsBySig.TryGetValue(signature, out var arm))
                {
                    arm = new ABIReversePInvokeMethodInfo()
                    {
                        Method = desc,
                        Signature = signature,
                        Count = 0,
                        Callvention = callingConv,
                    };
                    methodsBySig.Add(signature, arm);
                }
                int preserveCount = method.GenerationAttribute != null ? (int)method.GenerationAttribute.ConstructorArguments[0].Value : 1;
                arm.Count += preserveCount;
            }
            var methods = methodsBySig.Values.ToList();
            methods.Sort((a, b) => string.CompareOrdinal(a.Signature, b.Signature));
            return methods;
        }

        public void Run()
        {
            CollectReversePInvokeMethods();
        }
    }
}
