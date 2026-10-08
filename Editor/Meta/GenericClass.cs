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
using HybridCLR.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HybridCLR.Meta
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
            return new GenericClass(Type, MetaUtil.ToShareTypeSigs(Type.Module.CorLibTypes, KlassInst));
        }

        public override bool Equals(object obj)
        {
            if (obj is GenericClass gc)
            {
                return Type == gc.Type && MetaUtil.EqualsTypeSigArray(KlassInst, gc.KlassInst);
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

        public TypeSig ToTypeSig()
        {
            return new GenericInstSig(this.Type.ToTypeSig().ToClassOrValueTypeSig(), this.KlassInst);
        }

        public static GenericClass ResolveClass(TypeSpec type, GenericArgumentContext ctx)
        {
            var sig = type.TypeSig.ToGenericInstSig();
            if (sig == null)
            {
                return null;
            }
            TypeDef def = type.ResolveTypeDef();
            if (def == null)
            {
                Debug.LogWarning($"type:{type} ResolveTypeDef() == null");
                return null;
            }
            var klassInst = ctx != null ? sig.GenericArguments.Select(ga => MetaUtil.Inflate(ga, ctx)).ToList() : sig.GenericArguments.ToList();
            return new GenericClass(def, klassInst);
        }
    }
}
