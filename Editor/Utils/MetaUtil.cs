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

﻿using dnlib.DotNet;
using HybridCLR.Meta;
using HybridCLR.Mod;
using HybridCLR.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using IAssemblyResolver = HybridCLR.Mod.IAssemblyResolver;

namespace HybridCLR.Utils
{
    public static class MetaUtil
    {

        public static bool EqualsTypeSig(TypeSig a, TypeSig b)
        {
            if (a == b)
            {
                return true;
            }
            if (a != null && b != null)
            {
                return TypeEqualityComparer.Instance.Equals(a, b);
            }
            return false;
        }

        public static bool EqualsTypeSigArray(List<TypeSig> a, List<TypeSig> b)
        {
            if (a == b)
            {
                return true;
            }
            if (a != null && b != null)
            {
                if (a.Count != b.Count)
                {
                    return false;
                }
                for (int i = 0; i < a.Count; i++)
                {
                    if (!TypeEqualityComparer.Instance.Equals(a[i], b[i]))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        public static TypeSig Inflate(TypeSig sig, GenericArgumentContext ctx)
        {
            if (!sig.ContainsGenericParameter)
            {
                return sig;
            }
            return ctx.Resolve(sig);
        }

        public static TypeSig ToShareTypeSig(ICorLibTypes corTypes, TypeSig typeSig)
        {
            var a = typeSig.RemovePinnedAndModifiers();
            switch (a.ElementType)
            {
            case ElementType.Void: return corTypes.Void;
            case ElementType.Boolean: return corTypes.Byte;
            case ElementType.Char: return corTypes.UInt16;
            case ElementType.I1: return corTypes.SByte;
            case ElementType.U1: return corTypes.Byte;
            case ElementType.I2: return corTypes.Int16;
            case ElementType.U2: return corTypes.UInt16;
            case ElementType.I4: return corTypes.Int32;
            case ElementType.U4: return corTypes.UInt32;
            case ElementType.I8: return corTypes.Int64;
            case ElementType.U8: return corTypes.UInt64;
            case ElementType.R4: return corTypes.Single;
            case ElementType.R8: return corTypes.Double;
            case ElementType.String: return corTypes.Object;
            case ElementType.TypedByRef: return corTypes.TypedReference;
            case ElementType.I: return corTypes.IntPtr;
            case ElementType.U: return corTypes.UIntPtr;
            case ElementType.Object: return corTypes.Object;
            case ElementType.Sentinel: return typeSig;
            case ElementType.Ptr: return corTypes.UIntPtr;
            case ElementType.ByRef: return corTypes.UIntPtr;
            case ElementType.SZArray: return corTypes.Object;
            case ElementType.Array: return corTypes.Object;
            case ElementType.ValueType:
            {
                TypeDef typeDef = a.ToTypeDefOrRef().ResolveTypeDef();
                if (typeDef == null)
                {
                    throw new Exception($"type:{a} definition could not be found");
                }
                if (typeDef.IsEnum)
                {
                    return ToShareTypeSig(corTypes, typeDef.GetEnumUnderlyingType());
                }
                return typeSig;
            }
            case ElementType.Var:
            case ElementType.MVar:
            case ElementType.Class: return corTypes.Object;
            case ElementType.GenericInst:
            {
                var gia = (GenericInstSig)a;
                TypeDef typeDef = gia.GenericType.ToTypeDefOrRef().ResolveTypeDef();
                if (typeDef == null)
                {
                    throw new Exception($"type:{a} definition could not be found");
                }
                if (typeDef.IsEnum)
                {
                    return ToShareTypeSig(corTypes, typeDef.GetEnumUnderlyingType());
                }
                if (!typeDef.IsValueType)
                {
                    return corTypes.Object;
                }
                return new GenericInstSig(gia.GenericType, gia.GenericArguments.Select(ga => ToShareTypeSig(corTypes, ga)).ToList());
            }
            case ElementType.FnPtr: return corTypes.UIntPtr;
            case ElementType.ValueArray: return typeSig;
            case ElementType.Module: return typeSig;
            default:
            throw new NotSupportedException(typeSig.ToString());
            }
        }

        public static List<TypeSig> ToShareTypeSigs(ICorLibTypes corTypes, IList<TypeSig> typeSigs)
        {
            if (typeSigs == null)
            {
                return null;
            }
            return typeSigs.Select(s => ToShareTypeSig(corTypes, s)).ToList();
        }

        public static IAssemblyResolver CreateHotUpdateAssemblyResolver(BuildTarget target, List<string> hotUpdateDlls)
        {
            var externalDirs = HybridCLRSettings.instance.externalHotUpdateAssembliyDirs;
            var defaultHotUpdateOutputDir = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);
            IAssemblyResolver defaultHotUpdateResolver = new FixedSetAssemblyResolver(defaultHotUpdateOutputDir, hotUpdateDlls);
            if (externalDirs == null || externalDirs.Length == 0)
            {
                return defaultHotUpdateResolver;
            }
            else
            {
                var resolvers = new List<IAssemblyResolver>();
                foreach (var dir in externalDirs)
                {
                    resolvers.Add(new FixedSetAssemblyResolver($"{dir}/{target}", hotUpdateDlls));
                    resolvers.Add(new FixedSetAssemblyResolver(dir, hotUpdateDlls));
                }
                resolvers.Add(defaultHotUpdateResolver);
                return new CombinedAssemblyResolver(resolvers.ToArray());
            }
        }

        public static IAssemblyResolver CreateAOTAssemblyResolver(BuildTarget target)
        {
            return new PathAssemblyResolver(SettingsUtil.GetAssembliesPostIl2CppStripDir(target));
        }

        public static IAssemblyResolver CreateHotUpdateAndAOTAssemblyResolver(BuildTarget target, List<string> hotUpdateDlls)
        {
            return new CombinedAssemblyResolver(
                CreateHotUpdateAssemblyResolver(target, hotUpdateDlls),
                CreateAOTAssemblyResolver(target)
                );
        }

        public static string ResolveNetStandardAssemblyPath(string assemblyName)
        {
            return $"{SettingsUtil.HybridCLRDataPathInPackage}/NetStandard/{assemblyName}.dll";
        }


        public static List<TypeSig> CreateDefaultGenericParams(ModuleDef module, int genericParamCount)
        {
            var methodGenericParams = new List<TypeSig>();
            for (int i = 0; i < genericParamCount; i++)
            {
                methodGenericParams.Add(module.CorLibTypes.Object);
            }
            return methodGenericParams;
        }

        public static bool IsSupportedPInvokeTypeSig(TypeSig typeSig)
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

        public static bool IsSupportedPInvokeMethodSignature(MethodSig methodSig)
        {
            return IsSupportedPInvokeTypeSig(methodSig.RetType) && methodSig.Params.All(p => IsSupportedPInvokeTypeSig(p));
        }
    }
}
