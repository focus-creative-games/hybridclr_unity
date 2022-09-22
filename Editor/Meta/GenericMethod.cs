using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HybridCLR.Editor.Meta
{
    public class GenericMethod
    {
        public MethodDef Method { get; }

        public List<TypeSig> KlassInst { get; }

        public List<TypeSig> MethodInst { get; }

        private readonly int _hashCode;

        public GenericMethod(MethodDef method, List<TypeSig> classInst, List<TypeSig> methodInst)
        {
            Method = method;
            KlassInst = classInst;
            MethodInst = methodInst;
            _hashCode = ComputHashCode();
        }

        public GenericMethod ToGenericShare()
        {
            return new GenericMethod(Method, MetaUtil.ToShareTypeSigs(KlassInst), MetaUtil.ToShareTypeSigs(MethodInst));
        }

        public override bool Equals(object obj)
        {
            GenericMethod o = (GenericMethod)obj;
            return Method == o.Method
                && EqualityUtil.EqualsTypeSigArray(KlassInst, o.KlassInst)
                && EqualityUtil.EqualsTypeSigArray(MethodInst, o.MethodInst);
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        private int ComputHashCode()
        {
            int hash = MethodEqualityComparer.CompareDeclaringTypes.GetHashCode(Method);
            if (KlassInst != null)
            {
                hash = HashUtil.CombineHash(hash, HashUtil.ComputHash(KlassInst));
            }
            if (MethodInst != null)
            {
                hash = HashUtil.CombineHash(hash, HashUtil.ComputHash(MethodInst));
            }
            return hash;
        }

        public static GenericMethod ResolveMethod(IMethod method, GenericArgumentContext ctx)
        {
            //Debug.Log($"== resolve method:{method}");
            TypeDef typeDef = null;
            List<TypeSig> klassInst = null;
            List<TypeSig> methodInst = null;

            MethodDef methodDef = null;


            var decalringType = method.DeclaringType;
            typeDef = decalringType.ResolveTypeDef();
            if (typeDef == null)
            {
                throw new Exception($"{decalringType}");
            }
            GenericInstSig gis = decalringType.TryGetGenericInstSig();
            if (gis != null)
            {
                klassInst = ctx != null ? gis.GenericArguments.Select(ga => MetaUtil.Inflate(ga, ctx)).ToList() : gis.GenericArguments.ToList();
            }
            methodDef = method.ResolveMethodDef();
            if (methodDef == null)
            {
                return null;
            }
            if (method is MethodSpec methodSpec)
            {
                methodInst = ctx != null ? methodSpec.GenericInstMethodSig.GenericArguments.Select(ga => MetaUtil.Inflate(ga, ctx)).ToList()
                    : methodSpec.GenericInstMethodSig.GenericArguments.ToList();
            }
            return new GenericMethod(methodDef, klassInst, methodInst);
        }

    }
}
