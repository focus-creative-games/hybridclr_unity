using dnlib.DotNet;
using HybridCLR.Editor.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HybridCLR.Editor.ABI
{
    public class SingletonStruct
    {
        public TypeSig Type { get; set; }
    }

    public class TypeCreatorWebGL32 : TypeCreatorBase
    {
        public override bool IsArch32 => true;

        protected override TypeInfo OptimizeSigType(TypeInfo type, bool returnType)
        {
            //if (type.PorType > ParamOrReturnType.STRUCTURE_ALIGN1 && type.PorType <= ParamOrReturnType.STRUCTURE_ALIGN4)
            //{
            //    return new TypeInfo(ParamOrReturnType.STRUCTURE_ALIGN1, type.Size);
            //}
            return type;
        }

        private static bool IsEmptyOrSpeicalValueType(TypeSig type)
        {
            TypeDef typeDef = type.ToTypeDefOrRef().ResolveTypeDefThrow();
            if (typeDef.IsEnum)
            {
                return false;
            }
            var fields = typeDef.Fields;// typeDef.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fields.Count == 0)
            {
                return true;
            }
            if (fields.All(f => f.IsStatic))
            {
                return true;
            }
            if (typeDef.IsExplicitLayout && fields.Count(f => !f.IsStatic) > 1)
            {
                return true;
            }
            return false;
        }

        public static bool TryComputSingletonStructTypeInfo(TypeSig type, int typeAligment, int typeSize, out TypeInfo typeInfo)
        {
            typeInfo = null;
            if (typeAligment > 8 || typeSize > 8)
            {
                return false;
            }
            var ss = new SingletonStruct();
            if (!TryComputSingletonStruct0(type, ss) || ss.Type == null)
            {
                return false;
            }
            if (ss.Type.IsByRef)
            {
                typeInfo = TypeInfo.s_i4;
                return true;
            }
            switch (ss.Type.ElementType)
            {
                case ElementType.Boolean:
                case ElementType.Char:
                case ElementType.I1:
                case ElementType.U1:
                case ElementType.I2:
                case ElementType.U2:
                case ElementType.I4:
                case ElementType.U4:
                case ElementType.I:
                case ElementType.U:
                case ElementType.String:
                case ElementType.Ptr:
                case ElementType.Class:
                case ElementType.Array:
                case ElementType.GenericInst:
                case ElementType.FnPtr:
                case ElementType.Object:
                case ElementType.SZArray:
                    {
                        if (typeAligment <= 4 && typeSize <= 4)
                        {
                            typeInfo = TypeInfo.s_i4;
                        }
                        break;
                    }
                case ElementType.I8:
                case ElementType.U8:
                    {
                        if (typeAligment <= 8 && typeSize <= 8)
                        {
                            typeInfo = TypeInfo.s_i8;
                        }
                        break;
                    }
                case ElementType.R4:
                    {
                        if (typeAligment <= 4 && typeSize <= 4)
                        {
                            typeInfo = TypeInfo.s_r4;
                        }
                        break;
                    }
                case ElementType.R8:
                    {
                        if (typeAligment <= 8 && typeSize <= 8)
                        {
                            typeInfo = TypeInfo.s_r8;
                        }
                        break;
                    }
                default: return false;
            }
            return typeInfo != null;
        }

        public static bool TryComputSingletonStruct(TypeSig type, out SingletonStruct result)
        {
            result = new SingletonStruct();
            return TryComputSingletonStruct0(type, result) && result.Type != null;
        }

        public static bool TryComputSingletonStruct0(TypeSig type, SingletonStruct result)
        {
            TypeDef typeDef = type.ToTypeDefOrRef().ResolveTypeDefThrow();
            if (typeDef.IsEnum)
            {
                if (result.Type == null)
                {
                    result.Type = typeDef.GetEnumUnderlyingType();
                    return true;
                }
                else
                {
                    return false;
                }
            }

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

                switch (ftype.ElementType)
                {
                    case ElementType.TypedByRef: return false;
                    case ElementType.ValueType:
                        {
                            if (!TryComputSingletonStruct0(ftype, result))
                            {
                                return false;
                            }
                            break;
                        }
                    case ElementType.GenericInst:
                        {
                            if (!ftype.IsValueType)
                            {
                                goto default;
                            }
                            if (!TryComputSingletonStruct0(ftype, result))
                            {
                                return false;
                            }
                            break;
                        }
                    default:
                        {
                            if (result.Type != null)
                            {
                                return false;
                            }
                            result.Type = ftype;
                            break;
                        }
                }
            }

            return true;
        }

        protected override bool TryCreateCustomValueTypeInfo(TypeSig type, int typeSize, int typeAligment, out TypeInfo typeInfo)
        {
            typeInfo = null;
            if (IsEmptyOrSpeicalValueType(type))
            {
                switch (typeAligment)
                {
                    case 1: typeInfo = new TypeInfo(ParamOrReturnType.SPECIAL_STRUCTURE_ALIGN1, typeSize); break;
                    case 2: typeInfo = new TypeInfo(ParamOrReturnType.SPECIAL_STRUCTURE_ALIGN2, typeSize); break;
                    case 4: typeInfo = new TypeInfo(ParamOrReturnType.SPECIAL_STRUCTURE_ALIGN4, typeSize); break;
                    case 8: typeInfo = new TypeInfo(ParamOrReturnType.SPECIAL_STRUCTURE_ALIGN8, typeSize); break;
                    default: throw new NotSupportedException();
                }
                return true;
            }
            if (TryComputSingletonStructTypeInfo(type, typeAligment, typeSize, out typeInfo))
            {
                return true;
            }
            return false;
        }
    }
}
