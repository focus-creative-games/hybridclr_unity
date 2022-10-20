﻿using dnlib.DotNet;
using HybridCLR.Editor.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HybridCLR.Editor.Meta
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

		public static TypeSig ToShareTypeSig(TypeSig typeSig)
        {
			var corTypes = typeSig?.Module?.CorLibTypes;
			if (corTypes == null)
            {
				return typeSig;
            }
			var a = typeSig.RemovePinnedAndModifiers();
			switch (a.ElementType)
			{
				case ElementType.Void: return corTypes.Void;
				case ElementType.Boolean: return corTypes.Byte;
				case ElementType.Char: return corTypes.UInt16;
				case ElementType.I1: return corTypes.SByte;
				case ElementType.U1:return corTypes.Byte;
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
				case ElementType.Ptr: return corTypes.IntPtr;
				case ElementType.ByRef: return corTypes.IntPtr;
				case ElementType.SZArray: return corTypes.Object;
				case ElementType.Array: return corTypes.Object;
				case ElementType.ValueType: return typeSig;
				case ElementType.Class: return corTypes.Object;
				case ElementType.GenericInst:
                {
					var gia = (GenericInstSig)a;
					if (gia.GenericType.IsClassSig)
                    {
						return corTypes.Object;
                    }
					return new GenericInstSig(gia.GenericType, gia.GenericArguments.Select(ga => ToShareTypeSig(ga)).ToList());
				}
				case ElementType.FnPtr: return corTypes.IntPtr;
				case ElementType.ValueArray: return typeSig;
				case ElementType.Module: return typeSig;
				default:
					throw new NotSupportedException(typeSig.ToString());
			}
		}
	
		public static List<TypeSig> ToShareTypeSigs(IList<TypeSig> typeSigs)
        {
			if (typeSigs == null)
            {
				return null;
            }
			return typeSigs.Select(s => ToShareTypeSig(s)).ToList();
        }

		public static IAssemblyResolver CreateBuildTargetAssemblyResolver(UnityEditor.BuildTarget target)
        {
			return new CombinedAssemblyResolver(new PathAssemblyResolver(
				SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target)),
				new UnityPluginAssemblyResolver(),
				new UnityAOTAssemblyResolver(),
				new UnityEditorAssemblyResolver());
		}
    }
}
