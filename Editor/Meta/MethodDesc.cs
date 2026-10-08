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
using System.Collections.Generic;
using System.Text;

namespace HybridCLR.Meta
{
    public class MethodDesc : IEquatable<MethodDesc>
    {
        public string Sig { get; private set; }

        public MethodDef MethodDef { get; set; }

        public ReturnInfo ReturnInfo { get; set; }

        public List<ParamInfo> ParamInfos { get; set; }

        public void Init()
        {
            for (int i = 0; i < ParamInfos.Count; i++)
            {
                ParamInfos[i].Index = i;
            }
            Sig = CreateCallSigName();
        }

        public void TransfromSigTypes(Func<TypeInfo, bool, TypeInfo> transformer)
        {
            ReturnInfo.Type = transformer(ReturnInfo.Type, true);
            foreach (var paramType in ParamInfos)
            {
                paramType.Type = transformer(paramType.Type, false);
            }
        }

        public string CreateCallSigName()
        {
            var n = new StringBuilder();
            n.Append(ReturnInfo.Type.CreateSigName());
            foreach (var param in ParamInfos)
            {
                n.Append(param.Type.CreateSigName());
            }
            return n.ToString();
        }

        public string CreateInvokeSigName()
        {
            var n = new StringBuilder();
            n.Append(ReturnInfo.Type.CreateSigName());
            foreach (var param in ParamInfos)
            {
                n.Append(param.Type.CreateSigName());
            }
            return n.ToString();
        }

        public override bool Equals(object obj)
        {
            return Equals((MethodDesc)obj);
        }

        public bool Equals(MethodDesc other)
        {
            return Sig == other.Sig;
        }

        public override int GetHashCode()
        {
            return Sig.GetHashCode();
        }
    }
}
