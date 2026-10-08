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
using HybridCLR.Utils;
using System.Collections.Generic;

namespace HybridCLR.CodeGen.AOT
{

    public class ConstraintContext
    {
        public class ImplType
        {
            public TypeSig BaseType { get; }

            public List<TypeSig> Interfaces { get; }

            public bool ValueType { get; }

            private readonly int _hash;

            public ImplType(TypeSig baseType, List<TypeSig> interfaces, bool valueType)
            {
                BaseType = baseType;
                Interfaces = interfaces;
                ValueType = valueType;
                _hash = ComputHash();
            }

            public override bool Equals(object obj)
            {
                ImplType o = (ImplType)obj;
                return MetaUtil.EqualsTypeSig(this.BaseType, o.BaseType)
                    && MetaUtil.EqualsTypeSigArray(this.Interfaces, o.Interfaces)
                    && this.ValueType == o.ValueType;
            }

            public override int GetHashCode()
            {
                return _hash;
            }

            private int ComputHash()
            {
                int hash = 0;
                if (BaseType != null)
                {
                    hash = HashUtil.CombineHash(hash, TypeEqualityComparer.Instance.GetHashCode(BaseType));
                }
                if (Interfaces.Count > 0)
                {
                    hash = HashUtil.CombineHash(hash, HashUtil.ComputHash(Interfaces));
                }

                return hash;
            }
        }

        public HashSet<ImplType> ImplTypes { get; } = new HashSet<ImplType>();

        public GenericClass ApplyConstraints(GenericClass gc)
        {
            return gc;
        }

        public GenericMethod ApplyConstraints(GenericMethod gm)
        {
            return gm;
        }
    }
}
