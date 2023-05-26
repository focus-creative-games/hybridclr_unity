using dnlib.DotNet;
using HybridCLR.Editor.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HybridCLR.Editor.ABI
{
    public class HFATypeInfo
    {
        public TypeSig Type { get; set; }

        public int Count { get; set; }
    }

    public class TypeCreatorArm64 : TypeCreatorBase
    {
        public override bool IsArch32 => false;

        protected override TypeInfo OptimizeSigType(TypeInfo type, bool returnType)
        {
            if (!type.IsGeneralValueType)
            {
                return type;
            }
            int typeSize = type.Size;
            if (typeSize <= 8)
            {
                return TypeInfo.s_i8;
            }
            if (typeSize <= 16)
            {
                return TypeInfo.s_i16;
            }
            if (returnType)
            {
                return type.PorType != ParamOrReturnType.STRUCTURE_ALIGN1 ? new TypeInfo(ParamOrReturnType.STRUCTURE_ALIGN1, typeSize) : type;
            }
            return TypeInfo.s_ref;
        }


        private static bool IsNotHFAFastCheck(int typeSize)
        {
            return typeSize % 4 != 0 || typeSize > 32;
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
                switch (ftype.ElementType)
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
            if (ok && typeInfo.Count >= 1 && typeInfo.Count <= 4)
            {
                int fieldSize = typeInfo.Type.ElementType == ElementType.R4 ? 4 : 8;
                return typeSize == fieldSize * typeInfo.Count;
            }
            return false;
        }

        protected override bool TryCreateCustomValueTypeInfo(TypeSig type, int typeSize, int typeAligment, out TypeInfo typeInfo)
        {
            if (ComputHFATypeInfo(type, typeSize, out HFATypeInfo hfaTypeInfo))
            {
                bool isFloat = hfaTypeInfo.Type.ElementType == ElementType.R4;
                switch (hfaTypeInfo.Count)
                {
                    case 1: typeInfo = isFloat ? TypeInfo.s_r4 : TypeInfo.s_r8; break;
                    case 2: typeInfo = isFloat ? TypeInfo.s_vf2 : TypeInfo.s_vd2; break;
                    case 3: typeInfo = isFloat ? TypeInfo.s_vf3 : TypeInfo.s_vd3; break;
                    case 4: typeInfo = isFloat ? TypeInfo.s_vf4 : TypeInfo.s_vd4; break;
                    default: throw new NotSupportedException();
                }
                return true;
            }
            typeInfo = null;
            return false;
        }
    }
}
