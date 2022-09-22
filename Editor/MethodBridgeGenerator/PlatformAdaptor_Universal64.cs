using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace HybridCLR.Editor.MethodBridgeGenerator
{
    public class PlatformAdaptor_Universal64 : PlatformAdaptorBase
    {
        public PlatformABI CallConventionType { get; } = PlatformABI.Universal64;

        public override bool IsArch32 => false;

        public override bool IsSupportHFA => true;

        protected override TypeInfo OptimizeSigType(TypeInfo type, bool returnType)
        {
            if (type.PorType > ParamOrReturnType.STRUCTURE_ALIGN1 && type.PorType <= ParamOrReturnType.STRUCTURE_ALIGN8)
            {
                return new TypeInfo(ParamOrReturnType.STRUCTURE_ALIGN1, type.Size);
            }
            return type;
        }

        public override void GenerateManaged2NativeMethod(MethodBridgeSig method, List<string> lines)
        {
            int totalQuadWordNum = method.ParamInfos.Count + method.ReturnInfo.GetParamSlotNum(this.CallConventionType);

            string paramListStr = string.Join(", ", method.ParamInfos.Select(p => $"{p.Type.GetTypeName()} __arg{p.Index}").Concat(new string[] { "const MethodInfo* method" }));
            string paramTypeListStr = string.Join(", ", method.ParamInfos.Select(p => $"{p.Type.GetTypeName()}").Concat(new string[] { "const MethodInfo*" })); ;
            string paramNameListStr = string.Join(", ", method.ParamInfos.Select(p => p.Managed2NativeParamValue(this.CallConventionType)).Concat(new string[] { "method" }));

            lines.Add($@"
// {method.MethodDef}
static void __M2N_{method.CreateCallSigName()}(const MethodInfo* method, uint16_t* argVarIndexs, StackObject* localVarBase, void* ret)
{{
    typedef {method.ReturnInfo.Type.GetTypeName()} (*NativeMethod)({paramListStr});
    {(!method.ReturnInfo.IsVoid ? $"*({method.ReturnInfo.Type.GetTypeName()}*)ret = " : "")}((NativeMethod)(GetInterpreterDirectlyCallMethodPointer(method)))({paramNameListStr});
}}
");
        }
        public override void GenerateNative2ManagedMethod(MethodBridgeSig method, List<string> lines)
        {
            int totalQuadWordNum = method.ParamInfos.Count + method.ReturnInfo.GetParamSlotNum(this.CallConventionType);
            string paramListStr = string.Join(", ", method.ParamInfos.Select(p => $"{p.Type.GetTypeName()} __arg{p.Index}").Concat(new string[] { "const MethodInfo* method" }));
            lines.Add($@"
// {method.MethodDef}
static {method.ReturnInfo.Type.GetTypeName()} __N2M_{method.CreateCallSigName()}({paramListStr})
{{
    StackObject args[{Math.Max(totalQuadWordNum, 1)}] = {{{string.Join(", ", method.ParamInfos.Select(p => p.Native2ManagedParamValue(this.CallConventionType)))} }};
    StackObject* ret = {(method.ReturnInfo.IsVoid ? "nullptr" : "args + " + method.ParamInfos.Count)};
    Interpreter::Execute(method, args, ret);
    {(!method.ReturnInfo.IsVoid ? $"return *({method.ReturnInfo.Type.GetTypeName()}*)ret;" : "")}
}}
");
        }

        public override void GenerateAdjustThunkMethod(MethodBridgeSig method, List<string> lines)
        {
            int totalQuadWordNum = method.ParamInfos.Count + method.ReturnInfo.GetParamSlotNum(this.CallConventionType);
            string paramListStr = string.Join(", ", method.ParamInfos.Select(p => $"{p.Type.GetTypeName()} __arg{p.Index}").Concat(new string[] { "const MethodInfo* method" }));
            lines.Add($@"
// {method.MethodDef}
static {method.ReturnInfo.Type.GetTypeName()} __N2M_AdjustorThunk_{method.CreateCallSigName()}({paramListStr})
{{
    StackObject args[{Math.Max(totalQuadWordNum, 1)}] = {{{string.Join(", ", method.ParamInfos.Select(p => (p.Index == 0 ? $"(uint64_t)(*(uint8_t**)&__arg{p.Index} + sizeof(Il2CppObject))" : p.Native2ManagedParamValue(this.CallConventionType))))} }};
    StackObject* ret = {(method.ReturnInfo.IsVoid ? "nullptr" : "args + " + method.ParamInfos.Count)};
    Interpreter::Execute(method, args, ret);
    {(!method.ReturnInfo.IsVoid ? $"return *({method.ReturnInfo.Type.GetTypeName()}*)ret;" : "")}
}}
");
        }
    }
}
