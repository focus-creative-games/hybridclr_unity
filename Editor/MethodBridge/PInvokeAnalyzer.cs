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

    public class PInvokeAnalyzer
    {
        private readonly List<ModuleDefMD> _rootModules = new List<ModuleDefMD>();

        private readonly List<CallNativeMethodSignatureInfo> _pinvokeMethodSignatures = new List<CallNativeMethodSignatureInfo>();

        public List<CallNativeMethodSignatureInfo> PInvokeMethodSignatures => _pinvokeMethodSignatures;

        public PInvokeAnalyzer(AssemblyCache cache, List<string> assemblyNames)
        {
            foreach (var assemblyName in assemblyNames)
            {
                _rootModules.Add(cache.LoadModule(assemblyName));
            }
        }

        private static bool IsSupportedPInvokeTypeSig(TypeSig typeSig)
        {
            typeSig = typeSig.RemovePinnedAndModifiers();
            if (typeSig.IsByRef)
            {
                return true;
            }
            switch (typeSig.ElementType)
            {
                case ElementType.SZArray:
                case ElementType.Array:
                //case ElementType.Class:
                case ElementType.String:
                //case ElementType.Object:
                return false;
                default: return true;
            }
        }

        private static bool IsSupportedPInvokeMethodSignature(MethodSig methodSig)
        {
            return IsSupportedPInvokeTypeSig(methodSig.RetType) && methodSig.Params.All(p => IsSupportedPInvokeTypeSig(p));
        }

        public void Run()
        {
            foreach (var mod in _rootModules)
            {
                foreach (TypeDef type in mod.GetTypes())
                {
                    foreach (MethodDef method in type.Methods)
                    {
                        if (method.IsPinvokeImpl)
                        {
                            if (!IsSupportedPInvokeMethodSignature(method.MethodSig))
                            {
                                throw new Exception($"PInvoke method {method.FullName} has unsupported parameter or return type. Please check the method signature.");
                            }
                            _pinvokeMethodSignatures.Add(new CallNativeMethodSignatureInfo { MethodSig = method.MethodSig });
                        }
                    }
                }
            }
        }
    }
}
