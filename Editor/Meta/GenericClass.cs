using dnlib.DotNet;
using System.Collections.Generic;
using System.Linq;

namespace HybridCLR.Editor.Meta
{
    public class GenericClass
    {
        public TypeDef Type { get; }

        public List<TypeSig> KlassInst { get; }

        private readonly int _hashCode;

        public GenericClass(TypeDef type, List<TypeSig> classInst)
        {
            Type = type;
            KlassInst = classInst;
            _hashCode = ComputHashCode();
        }

        public GenericClass ToGenericShare()
        {
            return new GenericClass(Type, MetaUtil.ToShareTypeSigs(KlassInst));
        }

        public override bool Equals(object obj)
        {
            if (obj is GenericClass gc)
            {
                return Type == gc.Type && EqualityUtil.EqualsTypeSigArray(KlassInst, gc.KlassInst);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        private int ComputHashCode()
        {
            int hash = TypeEqualityComparer.Instance.GetHashCode(Type);
            if (KlassInst != null)
            {
                hash = HashUtil.CombineHash(hash, HashUtil.ComputHash(KlassInst));
            }
            return hash;
        }

        public static GenericClass ResolveClass(TypeSpec type, GenericArgumentContext ctx)
        {
            var sig = type.TypeSig.ToGenericInstSig();
            if (sig == null)
            {
                return null;
            }
            TypeDef def = type.ResolveTypeDef();
            var klassInst = ctx != null ? sig.GenericArguments.Select(ga => MetaUtil.Inflate(ga, ctx)).ToList() : sig.GenericArguments.ToList();
            return new GenericClass(def, klassInst);
        }
    }
}
