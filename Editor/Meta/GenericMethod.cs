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
using System;
using System.Collections.Generic;
using System.Linq;

namespace HybridCLR.Meta
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
            ICorLibTypes corLibTypes = Method.Module.CorLibTypes;
            return new GenericMethod(Method, MetaUtil.ToShareTypeSigs(corLibTypes, KlassInst), MetaUtil.ToShareTypeSigs(corLibTypes, MethodInst));
        }

        public override bool Equals(object obj)
        {
            GenericMethod o = (GenericMethod)obj;
            return Method == o.Method
                && MetaUtil.EqualsTypeSigArray(KlassInst, o.KlassInst)
                && MetaUtil.EqualsTypeSigArray(MethodInst, o.MethodInst);
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        public override string ToString()
        {
            return $"{Method}|{string.Join(",", (IEnumerable<TypeSig>)KlassInst ?? Array.Empty<TypeSig>())}|{string.Join(",", (IEnumerable<TypeSig>)MethodInst ?? Array.Empty<TypeSig>())}";
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

        public MethodSpec ToMethodSpec()
        {
            IMethodDefOrRef mt = KlassInst != null ?
                (IMethodDefOrRef)new MemberRefUser(this.Method.Module, Method.Name, Method.MethodSig, new TypeSpecUser(new GenericInstSig(this.Method.DeclaringType.ToTypeSig().ToClassOrValueTypeSig(), this.KlassInst)))
                : this.Method;
            return new MethodSpecUser(mt, new GenericInstMethodSig(MethodInst));
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
                return null;
            }
            GenericInstSig gis = decalringType.TryGetGenericInstSig();
            if (gis != null)
            {
                klassInst = ctx != null ? gis.GenericArguments.Select(ga => MetaUtil.Inflate(ga, ctx)).ToList() : gis.GenericArguments.ToList();
            }
            methodDef = method.ResolveMethodDef();
            if (methodDef == null)
            {
                //Debug.LogWarning($"method:{method} ResolveMethodDef() == null");
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
