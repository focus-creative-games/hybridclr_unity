using dnlib.DotNet;
using HybridCLR.Editor.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace HybridCLR.Editor.MethodBridgeGenerator
{
    public abstract class PlatformAdaptorBase
    {
        private static readonly ValueTypeSizeAligmentCalculator s_calculator64 = new ValueTypeSizeAligmentCalculator(false);

        private static readonly ValueTypeSizeAligmentCalculator s_calculator32 = new ValueTypeSizeAligmentCalculator(true);

        public abstract bool IsArch32 { get; }

        public virtual bool IsSupportHFA => false;

        public TypeInfo GetNativeIntTypeInfo() => IsArch32 ? TypeInfo.s_i4u4 : TypeInfo.s_i8u8;

        public abstract void GenerateManaged2NativeMethod(MethodBridgeSig method, List<string> lines);

        public abstract void GenerateNative2ManagedMethod(MethodBridgeSig method, List<string> lines);

        public abstract void GenerateAdjustThunkMethod(MethodBridgeSig method, List<string> outputLines);

        protected abstract TypeInfo OptimizeSigType(TypeInfo type, bool returnType);

        public virtual void OptimizeMethod(MethodBridgeSig method)
        {
            method.TransfromSigTypes(OptimizeSigType);
        }

        private readonly Dictionary<TypeSig, (int, int)> _typeSizeCache64 = new Dictionary<TypeSig, (int, int)>(TypeEqualityComparer.Instance);

        private readonly Dictionary<TypeSig, (int, int)> _typeSizeCache32 = new Dictionary<TypeSig, (int, int)>(TypeEqualityComparer.Instance);

        public (int Size, int Aligment) ComputeSizeAndAligmentOfArch64(TypeSig t)
        {
            if (_typeSizeCache64.TryGetValue(t, out var sizeAndAligment))
            {
                return sizeAndAligment;
            }
            sizeAndAligment = s_calculator64.SizeAndAligmentOf(t);
            _typeSizeCache64.Add(t, sizeAndAligment);
            return sizeAndAligment;
        }

        protected (int Size, int Aligment) ComputeSizeAndAligmentOfArch32(TypeSig t)
        {
            if (_typeSizeCache32.TryGetValue(t, out var sa))
            {
                return sa;
            }
            // all this just to invoke one opcode with no arguments!
            sa = s_calculator32.SizeAndAligmentOf(t);
            _typeSizeCache32.Add(t, sa);
            return sa;
        }

        public TypeInfo CreateTypeInfo(TypeSig type)
        {
            type = type.RemovePinnedAndModifiers();
            if (type.IsByRef)
            {
                return GetNativeIntTypeInfo();
            }
            switch(type.ElementType)
            {
                case ElementType.Void: return TypeInfo.s_void;
                case ElementType.Boolean:
                case ElementType.I1:
                case ElementType.U1: return TypeInfo.s_i1u1;
                case ElementType.Char:
                case ElementType.I2:
                case ElementType.U2: return TypeInfo.s_i2u2;
                case ElementType.I4:
                case ElementType.U4: return TypeInfo.s_i4u4;
                case ElementType.I8:
                case ElementType.U8: return TypeInfo.s_i8u8;
                case ElementType.R4: return TypeInfo.s_r4;
                case ElementType.R8: return TypeInfo.s_r8;
                case ElementType.I:
                case ElementType.U:
                case ElementType.String: 
                case ElementType.Ptr:
                case ElementType.ByRef:
                case ElementType.Class:
                case ElementType.Array:
                case ElementType.SZArray:
                case ElementType.FnPtr:
                case ElementType.Object:
                case ElementType.Module: return GetNativeIntTypeInfo();
                case ElementType.TypedByRef: return CreateValueType(type);
                case ElementType.ValueType:
                {
                    TypeDef typeDef = type.ToTypeDefOrRef().ResolveTypeDef();
                    if (typeDef.IsEnum)
                    {
                        return CreateTypeInfo(typeDef.GetEnumUnderlyingType());
                    }
                    return CreateValueType(type);
                }
                case ElementType.GenericInst:
                {
                    GenericInstSig gis = (GenericInstSig)type;
                    if (!gis.GenericType.IsValueType)
                    {
                        return GetNativeIntTypeInfo();
                    }
                    TypeDef typeDef = gis.GenericType.ToTypeDefOrRef().ResolveTypeDef();
                    if (typeDef.IsEnum)
                    {
                        return CreateTypeInfo(typeDef.GetEnumUnderlyingType());
                    }
                    return CreateValueType(type);
                }
                default: throw new NotSupportedException($"{type.ElementType}");
            }
        }

        private static bool IsNotHFAFastCheck(int typeSize)
        {
            return typeSize != 8 && typeSize != 12 && typeSize != 16 && typeSize != 24 && typeSize != 32;
        }

        private static bool ComputHFATypeInfo0(TypeSig type, HFATypeInfo typeInfo)
        {
            TypeDef typeDef = type.ToTypeDefOrRef().ResolveTypeDefThrow();

            List<TypeSig> klassInst = type.ToGenericInstSig()?.GenericArguments?.ToList();
            GenericArgumentContext ctx = klassInst != null ? new GenericArgumentContext(klassInst, null) : null;

            var fields = typeDef.Fields;// typeDef.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (FieldDef field in fields)
            {
                if (field.IsStatic)
                {
                    continue;
                }
                TypeSig ftype = ctx != null ? MetaUtil.Inflate(field.FieldType, ctx) : field.FieldType;
                switch(ftype.ElementType)
                {
                    case ElementType.R4:
                    case ElementType.R8:
                    {
                        if (ftype == typeInfo.Type || typeInfo.Type == null)
                        {
                            typeInfo.Type = ftype;
                            ++typeInfo.Count;
                        }
                        else
                        {
                            return false;
                        }
                        break;
                    }
                    case ElementType.ValueType:
                    {
                        if (!ComputHFATypeInfo0(ftype, typeInfo))
                        {
                            return false;
                        }
                        break;
                    }
                    case ElementType.GenericInst:
                    {
                        if (!ftype.IsValueType || !ComputHFATypeInfo0(ftype, typeInfo))
                        {
                            return false;
                        }
                        break;
                    }
                    default: return false;
                }
            }
            return typeInfo.Count <= 4;
        }

        private static bool ComputHFATypeInfo(TypeSig type, int typeSize, out HFATypeInfo typeInfo)
        {
            typeInfo = new HFATypeInfo();
            if (IsNotHFAFastCheck(typeSize))
            {
                return false;
            }
            bool ok = ComputHFATypeInfo0(type, typeInfo);
            if (ok && typeInfo.Count >= 2 && typeInfo.Count <= 4)
            {
                int fieldSize = typeInfo.Type.ElementType == ElementType.R4 ? 4 : 8;
                return typeSize == fieldSize * typeInfo.Count;
            }
            return false;
        }

        protected static TypeInfo CreateGeneralValueType(TypeSig type, int size, int aligment)
        {
            Debug.Assert(size % aligment == 0);
            switch (aligment)
            {
                case 1: return new TypeInfo(ParamOrReturnType.STRUCTURE_ALIGN1, size);
                case 2: return new TypeInfo(ParamOrReturnType.STRUCTURE_ALIGN2, size);
                case 4: return new TypeInfo(ParamOrReturnType.STRUCTURE_ALIGN4, size);
                case 8: return new TypeInfo(ParamOrReturnType.STRUCTURE_ALIGN8, size);
                default: throw new NotSupportedException($"type:{type} not support aligment:{aligment}");
            }
        }

        protected TypeInfo CreateValueType(TypeSig type)
        {
            (int typeSize, int typeAligment) = IsArch32 ? ComputeSizeAndAligmentOfArch32(type) : ComputeSizeAndAligmentOfArch64(type);
            if (IsSupportHFA && ComputHFATypeInfo(type, typeSize, out HFATypeInfo hfaTypeInfo))
            {
                bool isFloat = hfaTypeInfo.Type.ElementType == ElementType.R4;
                switch (hfaTypeInfo.Count)
                {
                    case 2: return isFloat ? TypeInfo.s_vf2 : TypeInfo.s_vd2;
                    case 3: return isFloat ? TypeInfo.s_vf3 : TypeInfo.s_vd3;
                    case 4: return isFloat ? TypeInfo.s_vf4 : TypeInfo.s_vd4;
                    default: throw new NotSupportedException();
                }
            }
            else
            {
                // 64位下结构体内存对齐规则是一样的
                return CreateGeneralValueType(type, typeSize, typeAligment);
            }
        }

        public void GenerateManaged2NativeStub(List<MethodBridgeSig> methods, List<string> lines)
        {
            lines.Add($@"
Managed2NativeMethodInfo hybridclr::interpreter::g_managed2nativeStub[] = 
{{
");

            foreach (var method in methods)
            {
                lines.Add($"\t{{\"{method.CreateInvokeSigName()}\", __M2N_{method.CreateInvokeSigName()}}},");
            }

            lines.Add($"\t{{nullptr, nullptr}},");
            lines.Add("};");
        }

        public void GenerateNative2ManagedStub(List<MethodBridgeSig> methods, List<string> lines)
        {
            lines.Add($@"
Native2ManagedMethodInfo hybridclr::interpreter::g_native2managedStub[] = 
{{
");

            foreach (var method in methods)
            {
                lines.Add($"\t{{\"{method.CreateInvokeSigName()}\", (Il2CppMethodPointer)__N2M_{method.CreateInvokeSigName()}}},");
            }

            lines.Add($"\t{{nullptr, nullptr}},");
            lines.Add("};");
        }

        public void GenerateAdjustThunkStub(List<MethodBridgeSig> methods, List<string> lines)
        {
            lines.Add($@"
NativeAdjustThunkMethodInfo hybridclr::interpreter::g_adjustThunkStub[] = 
{{
");

            foreach (var method in methods)
            {
                lines.Add($"\t{{\"{method.CreateInvokeSigName()}\", (Il2CppMethodPointer)__N2M_AdjustorThunk_{method.CreateCallSigName()}}},");
            }

            lines.Add($"\t{{nullptr, nullptr}},");
            lines.Add("};");
        }
    }
}
