using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HybridCLR.Editor.MethodBridgeGenerator
{
    public class MethodBridgeSig : IEquatable<MethodBridgeSig>
    {
        public MethodDef MethodDef { get; set; }

        public ReturnInfo ReturnInfo { get; set; }

        public List<ParamInfo> ParamInfos { get; set; }

        public void Init()
        {
            for(int i = 0; i < ParamInfos.Count; i++)
            {
                ParamInfos[i].Index = i;
            }
        }

        public void TransfromSigTypes(Func<TypeInfo, bool, TypeInfo> transformer)
        {
            ReturnInfo.Type = transformer(ReturnInfo.Type, true);
            foreach(var paramType in ParamInfos)
            {
                paramType.Type = transformer(paramType.Type, false);
            }
        }

        public string CreateCallSigName()
        {
            var n = new StringBuilder();
            n.Append(ReturnInfo.Type.CreateSigName());
            foreach(var param in ParamInfos)
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
            return Equals((MethodBridgeSig)obj);
        }

        public bool Equals(MethodBridgeSig other)
        {
            if (other == null)
            {
                return false;
            }

            if (!ReturnInfo.Type.Equals(other.ReturnInfo.Type))
            {
                return false;
            }
            if (ParamInfos.Count != other.ParamInfos.Count)
            {
                return false;
            }
            for(int i = 0; i < ParamInfos.Count; i++)
            {
                if (!ParamInfos[i].Type.Equals(other.ParamInfos[i].Type))
                {
                    return false;
                }
            }
            return true;
        }

        public override int GetHashCode()
        {
            int hash = 17;

            hash = hash * 23  + ReturnInfo.Type.GetHashCode();

            foreach(var p in ParamInfos)
            {
                hash = hash * 23 + p.Type.GetHashCode();
            }

            return hash;
        }
    }
}
