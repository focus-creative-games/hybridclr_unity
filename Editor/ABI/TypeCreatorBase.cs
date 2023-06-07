using dnlib.DotNet;
using HybridCLR.Editor.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace HybridCLR.Editor.ABI
{
    public abstract class TypeCreatorBase
    {
        public abstract bool IsArch32 { get; }

        public TypeInfo GetNativeIntTypeInfo() => IsArch32 ? TypeInfo.s_i4 : TypeInfo.s_i8;

        public ValueTypeSizeAligmentCalculator Calculator => IsArch32 ? ValueTypeSizeAligmentCalculator.Caculator32 : ValueTypeSizeAligmentCalculator.Caculator64;


        private readonly Dictionary<TypeSig, (int, int)> _typeSizeCache = new Dictionary<TypeSig, (int, int)>(TypeEqualityComparer.Instance);


        private readonly Dictionary<TypeSig, TypeInfo> _typeInfoCache = new Dictionary<TypeSig, TypeInfo>(TypeEqualityComparer.Instance);

        public (int Size, int Aligment) ComputeSizeAndAligment(TypeSig t)
        {
            if (_typeSizeCache.TryGetValue(t, out var sizeAndAligment))
            {
                return sizeAndAligment;
            }
            sizeAndAligment = Calculator.SizeAndAligmentOf(t);
            _typeSizeCache.Add(t, sizeAndAligment);
            return sizeAndAligment;
        }

        public TypeInfo CreateTypeInfo(TypeSig type)
        {
            if (!_typeInfoCache.TryGetValue(type, out var typeInfo))
            {
                typeInfo = CreateTypeInfo0(type);
                _typeInfoCache.Add(type, typeInfo);
            }
            return new TypeInfo(typeInfo.PorType, typeInfo.Size);
        }

        TypeInfo CreateTypeInfo0(TypeSig type)
        {
            type = type.RemovePinnedAndModifiers();
            if (type.IsByRef)
            {
                return GetNativeIntTypeInfo();
            }
            switch (type.ElementType)
            {
                case ElementType.Void: return TypeInfo.s_void;
                case ElementType.Boolean: return TypeInfo.s_u1;
                case ElementType.I1: return TypeInfo.s_i1;
                case ElementType.U1: return TypeInfo.s_u1;
                case ElementType.I2: return TypeInfo.s_i2;
                case ElementType.Char:
                case ElementType.U2: return TypeInfo.s_u2;
                case ElementType.I4: return TypeInfo.s_i4;
                case ElementType.U4: return TypeInfo.s_u4;
                case ElementType.I8: return TypeInfo.s_i8;
                case ElementType.U8: return TypeInfo.s_u8;
                case ElementType.R4: return TypeInfo.s_r4;
                case ElementType.R8: return TypeInfo.s_r8;
                case ElementType.U: return IsArch32 ? TypeInfo.s_u4 : TypeInfo.s_u8;
                case ElementType.I:
                case ElementType.String:
                case ElementType.Ptr:
                case ElementType.ByRef:
                case ElementType.Class:
                case ElementType.Array:
                case ElementType.SZArray:
                case ElementType.FnPtr:
                case ElementType.Object:
                case ElementType.Module:
                case ElementType.Var:
                case ElementType.MVar:
                    return GetNativeIntTypeInfo();
                case ElementType.TypedByRef: return CreateValueType(type);
                case ElementType.ValueType:
                {
                    TypeDef typeDef = type.ToTypeDefOrRef().ResolveTypeDef();
                    if (typeDef == null)
                    {
                        throw new Exception($"type:{type} 未能找到定义。请尝试 `HybridCLR/Genergate/LinkXml`，然后Build一次生成AOT dll，再重新生成桥接函数");
                    }
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

        protected static TypeInfo CreateGeneralValueType(TypeSig type, int size, int aligment)
        {
            System.Diagnostics.Debug.Assert(size % aligment == 0);
            switch (aligment)
            {
                case 1: return new TypeInfo(ParamOrReturnType.STRUCTURE_ALIGN1, size);
                case 2: return new TypeInfo(ParamOrReturnType.STRUCTURE_ALIGN2, size);
                case 4: return new TypeInfo(ParamOrReturnType.STRUCTURE_ALIGN4, size);
                case 8: return new TypeInfo(ParamOrReturnType.STRUCTURE_ALIGN8, size);
                default: throw new NotSupportedException($"type:{type} not support aligment:{aligment}");
            }
        }

        protected virtual bool TryCreateCustomValueTypeInfo(TypeSig type, int typeSize, int typeAligment, out TypeInfo typeInfo)
        {
            typeInfo = null;
            return false;
        }

        protected TypeInfo CreateValueType(TypeSig type)
        {
            (int typeSize, int typeAligment) = ComputeSizeAndAligment(type);
            if (TryCreateCustomValueTypeInfo(type, typeSize, typeAligment, out var typeInfo))
            {
                //Debug.Log($"[{GetType().Name}] CustomeValueType:{type} => {typeInfo.CreateSigName()}");
                return typeInfo;
            }
            else
            {
                // 64位下结构体内存对齐规则是一样的
                return CreateGeneralValueType(type, typeSize, typeAligment);
            }
        }


        protected abstract TypeInfo OptimizeSigType(TypeInfo type, bool returnType);

        public virtual void OptimizeMethod(MethodDesc method)
        {
            method.TransfromSigTypes(OptimizeSigType);
        }
    }
}
