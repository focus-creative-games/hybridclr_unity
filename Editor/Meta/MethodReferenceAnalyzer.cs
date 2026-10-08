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
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace HybridCLR.Meta
{
    public class MethodReferenceAnalyzer
    {
        private readonly Action<MethodDef, List<TypeSig>, List<TypeSig>, GenericMethod> _onNewMethod;

        private readonly ConcurrentDictionary<MethodDef, List<IMethod>> _methodEffectInsts = new ConcurrentDictionary<MethodDef, List<IMethod>>();

        public MethodReferenceAnalyzer(Action<MethodDef, List<TypeSig>, List<TypeSig>, GenericMethod> onNewMethod)
        {
            _onNewMethod = onNewMethod;
        }

        public void WalkMethod(MethodDef method, List<TypeSig> klassGenericInst, List<TypeSig> methodGenericInst)
        {
            var ctx = new GenericArgumentContext(klassGenericInst, methodGenericInst);

            if (_methodEffectInsts.TryGetValue(method, out var effectInsts))
            {
                foreach (var met in effectInsts)
                {
                    var resolveMet = GenericMethod.ResolveMethod(met, ctx)?.ToGenericShare();
                    _onNewMethod(method, klassGenericInst, methodGenericInst, resolveMet);
                }
                return;
            }

            var body = method.Body;
            if (body == null || !body.HasInstructions)
            {
                return;
            }

            effectInsts = new List<IMethod>();
            foreach (var inst in body.Instructions)
            {
                if (inst.Operand == null)
                {
                    continue;
                }
                switch (inst.Operand)
                {
                case IMethod met:
                {
                    if (!met.IsMethod)
                    {
                        continue;
                    }
                    var resolveMet = GenericMethod.ResolveMethod(met, ctx)?.ToGenericShare();
                    if (resolveMet == null)
                    {
                        continue;
                    }
                    effectInsts.Add(met);
                    _onNewMethod(method, klassGenericInst, methodGenericInst, resolveMet);
                    break;
                }
                case ITokenOperand token:
                {
                    //GenericParamContext paramContext = method.HasGenericParameters || method.DeclaringType.HasGenericParameters ?
                    //            new GenericParamContext(method.DeclaringType, method) : default;
                    //method.Module.ResolveToken(token.MDToken, paramContext);
                    break;
                }
                }
            }
            _methodEffectInsts.TryAdd(method, effectInsts);
        }
    }
}
