using dnlib.DotNet;
using HybridCLR.Editor.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IAssemblyResolver = HybridCLR.Editor.Meta.IAssemblyResolver;

namespace HybridCLR.Editor
{
    public static class MetaUtil
    {
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
			switch (typeSig.ElementType)
			{
				case ElementType.Void: return corTypes.Void;
				case ElementType.Boolean: 
				case ElementType.Char:
				case ElementType.I1:
				case ElementType.U1:return corTypes.Byte;
				case ElementType.I2:
				case ElementType.U2: return corTypes.UInt16;
				case ElementType.I4:
				case ElementType.U4: return corTypes.UInt32;
				case ElementType.I8:
				case ElementType.U8: return corTypes.UInt64;
				case ElementType.R4: return corTypes.Single;
				case ElementType.R8: return corTypes.Double;
				case ElementType.String: return corTypes.Object;
				case ElementType.TypedByRef: return corTypes.TypedReference;
				case ElementType.I: 
				case ElementType.U: return corTypes.UIntPtr;
				case ElementType.Object: return corTypes.Object;
				case ElementType.Sentinel: return typeSig;
				case ElementType.Ptr: return corTypes.UIntPtr;
				case ElementType.ByRef: return corTypes.UIntPtr;
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
				case ElementType.FnPtr: return corTypes.UIntPtr;
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
				SettingsUtil.GetHotFixDllsOutputDirByTarget(target)), new UnityEditorAssemblyResolver());
		}
    }
}
