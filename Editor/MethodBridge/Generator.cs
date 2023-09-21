using dnlib.DotNet;
using HybridCLR.Editor.ABI;
using HybridCLR.Editor.Meta;
using HybridCLR.Editor.Template;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using TypeInfo = HybridCLR.Editor.ABI.TypeInfo;

namespace HybridCLR.Editor.MethodBridge
{
    public class Generator
    {
        public class Options
        {
            public string TemplateCode { get; set; }

            public string OutputFile { get; set; }

            public IReadOnlyCollection<GenericMethod> GenericMethods { get; set; }
        }

        private readonly List<GenericMethod> _genericMethods;

        private readonly string _templateCode;

        private readonly string _outputFile;

        private readonly TypeCreator _typeCreator;

        private readonly HashSet<MethodDesc> _managed2nativeMethodSet = new HashSet<MethodDesc>();

        private readonly HashSet<MethodDesc> _native2managedMethodSet = new HashSet<MethodDesc>();

        private readonly HashSet<MethodDesc> _adjustThunkMethodSet = new HashSet<MethodDesc>();

        public Generator(Options options)
        {
            List<(GenericMethod, string)> genericMethodInfo = options.GenericMethods.Select(m => (m, m.ToString())).ToList();
            genericMethodInfo.Sort((a, b) => string.CompareOrdinal(a.Item2, b.Item2));
            _genericMethods = genericMethodInfo.Select(m => m.Item1).ToList();
            
            _templateCode = options.TemplateCode;
            _outputFile = options.OutputFile;
            _typeCreator = new TypeCreator();
        }


        private MethodDesc CreateMethodDesc(MethodDef methodDef, bool forceRemoveThis, TypeSig returnType, List<TypeSig> parameters)
        {
            var paramInfos = new List<ParamInfo>();
            if (forceRemoveThis && !methodDef.IsStatic)
            {
                parameters.RemoveAt(0);
            }
            if (returnType.ContainsGenericParameter)
            {
                throw new Exception($"[PreservedMethod] method:{methodDef} has generic parameters");
            }
            foreach (var paramInfo in parameters)
            {
                if (paramInfo.ContainsGenericParameter)
                {
                    throw new Exception($"[PreservedMethod] method:{methodDef} has generic parameters");
                }
                paramInfos.Add(new ParamInfo() { Type = _typeCreator.CreateTypeInfo(paramInfo) });
            }
            var mbs = new MethodDesc()
            {
                MethodDef = methodDef,
                ReturnInfo = new ReturnInfo() { Type = returnType != null ? _typeCreator.CreateTypeInfo(returnType) : TypeInfo.s_void },
                ParamInfos = paramInfos,
            };
            return mbs;
        }

        private void AddManaged2NativeMethod(MethodDesc method)
        {
            method.Init();
            _managed2nativeMethodSet.Add(method);
        }

        private void AddNative2ManagedMethod(MethodDesc method)
        {
            method.Init();
            _native2managedMethodSet.Add(method);
        }

        private void AddAdjustThunkMethod(MethodDesc method)
        {
            method.Init();
            _adjustThunkMethodSet.Add(method);
        }

        private void ProcessMethod(MethodDef method, List<TypeSig> klassInst, List<TypeSig> methodInst)
        {
            if (method.IsPrivate || (method.IsAssembly && !method.IsPublic && !method.IsFamily))
            {
                if (klassInst == null && methodInst == null)
                {
                    return;
                }
                else
                {
                    //Debug.Log($"[PreservedMethod] method:{method}");
                }
            }
            ICorLibTypes corLibTypes = method.Module.CorLibTypes;
            TypeSig returnType;
            List<TypeSig> parameters;
            if (klassInst == null && methodInst == null)
            {
                if (method.HasGenericParameters)
                {
                    throw new Exception($"[PreservedMethod] method:{method} has generic parameters");
                }
                returnType = MetaUtil.ToShareTypeSig(corLibTypes, method.ReturnType);
                parameters = method.Parameters.Select(p => MetaUtil.ToShareTypeSig(corLibTypes, p.Type)).ToList();
            }
            else
            {
                var gc = new GenericArgumentContext(klassInst, methodInst);
                returnType = MetaUtil.ToShareTypeSig(corLibTypes, MetaUtil.Inflate(method.ReturnType, gc));
                parameters = method.Parameters.Select(p => MetaUtil.ToShareTypeSig(corLibTypes, MetaUtil.Inflate(p.Type, gc))).ToList();
            }

            var m2nMethod = CreateMethodDesc(method, false, returnType, parameters);
            AddManaged2NativeMethod(m2nMethod);

            if (method.IsVirtual)
            {
                if (method.DeclaringType.IsInterface)
                {
                    AddAdjustThunkMethod(m2nMethod);
                }
                //var adjustThunkMethod = CreateMethodDesc(method, true, returnType, parameters);
                AddNative2ManagedMethod(m2nMethod);
            }
            if (method.Name == "Invoke" && method.DeclaringType.IsDelegate)
            {
                var openMethod = CreateMethodDesc(method, true, returnType, parameters);
                AddNative2ManagedMethod(openMethod);
            }
        }

        public void PrepareMethods()
        {
            foreach(var method in _genericMethods)
            {
                ProcessMethod(method.Method, method.KlassInst, method.MethodInst);
            }
        }

        public void Generate()
        {
            var frr = new FileRegionReplace(_templateCode);

            List<string> lines = new List<string>(20_0000);

            List<MethodDesc> managed2NativeMethodList = _managed2nativeMethodSet.ToList();
            managed2NativeMethodList.Sort((a, b) => string.CompareOrdinal(a.Sig, b.Sig));

            List<MethodDesc> native2ManagedMethodList = _native2managedMethodSet.ToList();
            native2ManagedMethodList.Sort((a, b) => string.CompareOrdinal(a.Sig, b.Sig));

            List<MethodDesc> adjustThunkMethodList = _adjustThunkMethodSet.ToList();
            adjustThunkMethodList.Sort((a, b) => string.CompareOrdinal(a.Sig, b.Sig));
            
            Debug.LogFormat("== managed2native:{0} native2managed:{1} adjustThunk:{2}",
                managed2NativeMethodList.Count, native2ManagedMethodList.Count, adjustThunkMethodList.Count);


            var structTypeSet = new HashSet<TypeInfo>();
            CollectStructDefs(managed2NativeMethodList, structTypeSet);
            CollectStructDefs(native2ManagedMethodList, structTypeSet);
            CollectStructDefs(adjustThunkMethodList, structTypeSet);
            List<TypeInfo> structTypes = structTypeSet.ToList();
            structTypes.Sort((a, b) => a.TypeId - b.TypeId);

            var classInfos = new List<ClassInfo>();
            var classTypeSet = new HashSet<TypeInfo>();
            foreach (var type in structTypes)
            {
                GenerateClassInfo(type, classTypeSet, classInfos);
            }

            GenerateStructDefines(classInfos, lines);
            GenerateStructureSignatureStub(structTypes, lines);

            foreach(var method in managed2NativeMethodList)
            {
                GenerateManaged2NativeMethod(method, lines);
            }

            GenerateManaged2NativeStub(managed2NativeMethodList, lines);

            foreach (var method in native2ManagedMethodList)
            {
                GenerateNative2ManagedMethod(method, lines);
            }

            GenerateNative2ManagedStub(native2ManagedMethodList, lines);

            foreach (var method in adjustThunkMethodList)
            {
                GenerateAdjustThunkMethod(method, lines);
            }

            GenerateAdjustThunkStub(adjustThunkMethodList, lines);

            frr.Replace("CODE", string.Join("\n", lines));

            Directory.CreateDirectory(Path.GetDirectoryName(_outputFile));

            frr.Commit(_outputFile);
        }

        private void CollectStructDefs(List<MethodDesc> methods, HashSet<TypeInfo> structTypes)
        {
            foreach (var method in methods)
            {
                foreach(var paramInfo in method.ParamInfos)
                {
                    if (paramInfo.Type.IsStruct)
                    {
                        structTypes.Add(paramInfo.Type);
                        if (paramInfo.Type.Klass.ContainsGenericParameter)
                        {
                            throw new Exception($"[CollectStructDefs] method:{method.MethodDef} type:{paramInfo.Type.Klass} contains generic parameter");
                        }
                    }
                    
                }
                if (method.ReturnInfo.Type.IsStruct)
                {
                    structTypes.Add(method.ReturnInfo.Type);
                    if (method.ReturnInfo.Type.Klass.ContainsGenericParameter)
                    {
                        throw new Exception($"[CollectStructDefs] method:{method.MethodDef} type:{method.ReturnInfo.Type.Klass} contains generic parameter");
                    }
                }
            }
            
        }

        class FieldInfo
        {
            public FieldDef field;
            public TypeInfo type;
        }

        class ClassInfo
        {
            public TypeInfo type;

            public TypeDef typeDef;

            public List<FieldInfo> fields = new List<FieldInfo>();

            public ClassLayout layout;
        }

        private void GenerateClassInfo(TypeInfo type, HashSet<TypeInfo> typeSet, List<ClassInfo> classInfos)
        {
            if (!typeSet.Add(type))
            {
                return;
            }
            TypeSig typeSig = type.Klass;
            var fields = new List<FieldInfo>();

            TypeDef typeDef = typeSig.ToTypeDefOrRef().ResolveTypeDefThrow();

            List<TypeSig> klassInst = typeSig.ToGenericInstSig()?.GenericArguments?.ToList();
            GenericArgumentContext ctx = klassInst != null ? new GenericArgumentContext(klassInst, null) : null;

            ClassLayout sa = typeDef.ClassLayout;
           
            ICorLibTypes corLibTypes = typeDef.Module.CorLibTypes;
            foreach (FieldDef field in typeDef.Fields)
            {
                if (field.IsStatic)
                {
                    continue;
                }
                TypeSig fieldType = ctx != null ? MetaUtil.Inflate(field.FieldType, ctx) : field.FieldType;
                fieldType = MetaUtil.ToShareTypeSig(corLibTypes, fieldType);
                var fieldTypeInfo = _typeCreator.CreateTypeInfo(fieldType);
                if (fieldTypeInfo.IsStruct)
                {
                    GenerateClassInfo(fieldTypeInfo, typeSet, classInfos);
                }
                fields.Add(new FieldInfo { field = field, type = fieldTypeInfo });
            }
            classInfos.Add(new ClassInfo() { type = type, typeDef = typeDef, fields = fields, layout = sa });
        }

        private void GenerateStructDefines(List<ClassInfo> classInfos, List<string> lines)
        {
            foreach (var ci in classInfos)
            {
                lines.Add($"// {ci.type.Klass}");
                uint packingSize = ci.layout?.PackingSize ?? 0;
                if (packingSize != 0)
                {
                    lines.Add($"#pragma pack(push, {packingSize})");
                }
                uint classSize = ci.layout?.ClassSize ?? 0;
               
                if (ci.typeDef.IsExplicitLayout)
                {
                    lines.Add($"union {ci.type.GetTypeName()} {{");
                    if (classSize > 0)
                    {
                        lines.Add($"\tstruct {{ char __fieldSize_offsetPadding[{classSize}];}};");
                    }
                    int index = 0;
                    foreach (var field in ci.fields)
                    {
                        uint offset = field.field.FieldOffset.Value;
                        string fieldName = $"__{field.field.Name.Replace('<', '_').Replace('>', '_')}_{index}";
                        lines.Add("\t#pragma pack(push, 1)");
                        lines.Add($"\tstruct {{ {(offset > 0 ? $"char {fieldName}_offsetPadding[{offset}];" : "")}  {field.type.GetTypeName()} {fieldName};}};");
                        lines.Add($"\t#pragma pack(pop)");
                        lines.Add($"\tstruct {{ {field.type.GetTypeName()} {fieldName}_forAlignmentOnly;}};");
                        ++index;
                    }
                }
                else
                {
                    lines.Add($"{(classSize > 0 ? "union" : "struct")} {ci.type.GetTypeName()} {{");
                    if (classSize > 0)
                    {
                        lines.Add($"\tstruct {{ char __fieldSize_offsetPadding[{classSize}];}};");
                        lines.Add("\tstruct {");
                    }
                    int index = 0;
                    foreach (var field in ci.fields)
                    {
                        string fieldName = $"__{field.field.Name.Replace('<', '_').Replace('>', '_')}_{index}";
                        lines.Add($"\t{field.type.GetTypeName()} {fieldName};");
                        ++index;
                    }
                    if (classSize > 0)
                    {
                        lines.Add("\t};");
                    }
                }
                lines.Add("};");
                if (packingSize != 0)
                {
                    lines.Add($"#pragma pack(pop)");
                }
            }
        }

        public const string SigOfObj = "u";

        public static string ToFullName(TypeSig type)
        {
            type = type.RemovePinnedAndModifiers();
            switch (type.ElementType)
            {
                case ElementType.Void: return "v";
                case ElementType.Boolean: return "u1";
                case ElementType.I1: return "i1";
                case ElementType.U1: return "u1";
                case ElementType.I2: return "i2";
                case ElementType.Char:
                case ElementType.U2: return "u2";
                case ElementType.I4: return "i4";
                case ElementType.U4: return "u4";
                case ElementType.I8: return "i8";
                case ElementType.U8: return "u8";
                case ElementType.R4: return "r4";
                case ElementType.R8: return "r8";
                case ElementType.I: return "i";
                case ElementType.U:
                case ElementType.String:
                case ElementType.Ptr:
                case ElementType.ByRef:
                case ElementType.Class:
                case ElementType.Array:
                case ElementType.SZArray:
                case ElementType.FnPtr:
                case ElementType.Object:
                    return SigOfObj;
                case ElementType.Module:
                case ElementType.Var:
                case ElementType.MVar:
                    throw new NotSupportedException($"ToFullName type:{type}");
                case ElementType.TypedByRef: return TypeInfo.strTypedByRef;
                case ElementType.ValueType:
                {
                    TypeDef typeDef = type.ToTypeDefOrRef().ResolveTypeDef();
                    if (typeDef == null)
                    {
                        throw new Exception($"type:{type} definition could not be found. Please try `HybridCLR/Genergate/LinkXml`, then Build once to generate the AOT dll, and then regenerate the bridge function");
                    }
                    if (typeDef.IsEnum)
                    {
                        return ToFullName(typeDef.GetEnumUnderlyingType());
                    }
                    return ToValueTypeFullName((ClassOrValueTypeSig)type);
                }
                case ElementType.GenericInst:
                    {
                        GenericInstSig gis = (GenericInstSig)type;
                        if (!gis.GenericType.IsValueType)
                        {
                            return SigOfObj;
                        }
                        TypeDef typeDef = gis.GenericType.ToTypeDefOrRef().ResolveTypeDef();
                        if (typeDef.IsEnum)
                        {
                            return ToFullName(typeDef.GetEnumUnderlyingType());
                        }
                        return $"{ToValueTypeFullName(gis.GenericType)}<{string.Join(",", gis.GenericArguments.Select(a => ToFullName(a)))}>";
                    }
                default: throw new NotSupportedException($"{type.ElementType}");
            }
        }

        private static bool IsSystemOrUnityAssembly(ModuleDef module)
        {
            if (module.IsCoreLibraryModule == true)
            {
                return true;
            }
            string assName = module.Assembly.Name.String;
            return assName.StartsWith("System.") || assName.StartsWith("UnityEngine.");
        }

        private static string ToValueTypeFullName(ClassOrValueTypeSig type)
        {
            TypeDef typeDef = type.ToTypeDefOrRef().ResolveTypeDef();
            if (typeDef == null)
            {
                throw new Exception($"type:{type} resolve fail");
            }

            if (typeDef.DeclaringType != null)
            {
                return $"{ToValueTypeFullName((ClassOrValueTypeSig)typeDef.DeclaringType.ToTypeSig())}/{typeDef.Name}";
            }

            if (IsSystemOrUnityAssembly(typeDef.Module))
            {
                return type.FullName;
            }
            return $"{Path.GetFileNameWithoutExtension(typeDef.Module.Name)}:{typeDef.FullName}";
        }

        public void GenerateStructureSignatureStub(List<TypeInfo> types, List<string> lines)
        {
            lines.Add("FullName2Signature hybridclr::interpreter::g_fullName2SignatureStub[] = {");
            foreach (var type in types)
            {
                lines.Add($"\t{{\"{ToFullName(type.Klass)}\", \"{type.CreateSigName()}\"}},");
            }
            lines.Add("\t{ nullptr, nullptr},");
            lines.Add("};");
        }

        public void GenerateManaged2NativeStub(List<MethodDesc> methods, List<string> lines)
        {
            lines.Add($@"
Managed2NativeMethodInfo hybridclr::interpreter::g_managed2nativeStub[] = 
{{
");

            foreach (var method in methods)
            {
                lines.Add($"\t{{\"{method.CreateInvokeSigName()}\", __M2N_{method.CreateInvokeSigName()}}},");
            }

            lines.Add($"\t{{nullptr, nullptr}},");
            lines.Add("};");
        }

        public void GenerateNative2ManagedStub(List<MethodDesc> methods, List<string> lines)
        {
            lines.Add($@"
Native2ManagedMethodInfo hybridclr::interpreter::g_native2managedStub[] = 
{{
");

            foreach (var method in methods)
            {
                lines.Add($"\t{{\"{method.CreateInvokeSigName()}\", (Il2CppMethodPointer)__N2M_{method.CreateInvokeSigName()}}},");
            }

            lines.Add($"\t{{nullptr, nullptr}},");
            lines.Add("};");
        }

        public void GenerateAdjustThunkStub(List<MethodDesc> methods, List<string> lines)
        {
            lines.Add($@"
NativeAdjustThunkMethodInfo hybridclr::interpreter::g_adjustThunkStub[] = 
{{
");

            foreach (var method in methods)
            {
                lines.Add($"\t{{\"{method.CreateInvokeSigName()}\", (Il2CppMethodPointer)__N2M_AdjustorThunk_{method.CreateCallSigName()}}},");
            }

            lines.Add($"\t{{nullptr, nullptr}},");
            lines.Add("};");
        }

        private string GetManaged2NativePassParam(TypeInfo type, string varName)
        {
            return $"M2NFromValueOrAddress<{type.GetTypeName()}>({varName})";
        }

        private string GetNative2ManagedPassParam(TypeInfo type, string varName)
        {
            return type.NeedExpandValue() ? $"(uint64_t)({varName})" : $"N2MAsUint64ValueOrAddress<{type.GetTypeName()}>({varName})";
        }

        public void GenerateManaged2NativeMethod(MethodDesc method, List<string> lines)
        {
            string paramListStr = string.Join(", ", method.ParamInfos.Select(p => $"{p.Type.GetTypeName()} __arg{p.Index}").Concat(new string[] { "const MethodInfo* method" }));
            string paramNameListStr = string.Join(", ", method.ParamInfos.Select(p => GetManaged2NativePassParam(p.Type, $"localVarBase+argVarIndexs[{p.Index}]")).Concat(new string[] { "method" }));

            lines.Add($@"
static void __M2N_{method.CreateCallSigName()}(const MethodInfo* method, uint16_t* argVarIndexs, StackObject* localVarBase, void* ret)
{{
    typedef {method.ReturnInfo.Type.GetTypeName()} (*NativeMethod)({paramListStr});
    {(!method.ReturnInfo.IsVoid ? $"*({method.ReturnInfo.Type.GetTypeName()}*)ret = " : "")}((NativeMethod)(method->methodPointerCallByInterp))({paramNameListStr});
}}
");
        }

        public string GenerateArgumentSizeAndOffset(List<ParamInfo> paramInfos)
        {
            StringBuilder s = new StringBuilder();
            int index = 0;
            foreach (var param in paramInfos)
            {
                s.AppendLine($"\tconstexpr int __ARG_OFFSET_{index}__ = {(index > 0 ? $"__ARG_OFFSET_{index - 1}__ + __ARG_SIZE_{index-1}__" : "0")};");
                s.AppendLine($"\tconstexpr int __ARG_SIZE_{index}__ = (sizeof(__arg{index}) + 7)/8;");
                index++;
            }
            s.AppendLine($"\tconstexpr int __TOTAL_ARG_SIZE__ = {(paramInfos.Count > 0 ? $"__ARG_OFFSET_{index-1}__ + __ARG_SIZE_{index-1}__" : "1")};");
            return s.ToString();
        }

        public string GenerateCopyArgumentToInterpreterStack(List<ParamInfo> paramInfos)            
        {
            StringBuilder s = new StringBuilder();
            int index = 0;
            foreach (var param in paramInfos)
            {
                if (param.Type.IsPrimitiveType)
                {
                    if (param.Type.NeedExpandValue())
                    {
                        s.AppendLine($"\targs[__ARG_OFFSET_{index}__].u64 = __arg{index};");
                    }
                    else
                    {
                        s.AppendLine($"\t*({param.Type.GetTypeName()}*)(args + __ARG_OFFSET_{index}__) = __arg{index};");
                    }
                }
                else
                {
                    s.AppendLine($"\t*({param.Type.GetTypeName()}*)(args + __ARG_OFFSET_{index}__) = __arg{index};");
                }
                index++;
            }
            return s.ToString();
        }

        private void GenerateNative2ManagedMethod0(MethodDesc method, bool adjustorThunk, List<string> lines)
        {
            string paramListStr = string.Join(", ", method.ParamInfos.Select(p => $"{p.Type.GetTypeName()} __arg{p.Index}").Concat(new string[] { "const MethodInfo* method" }));
            lines.Add($@"
static {method.ReturnInfo.Type.GetTypeName()} __N2M_{(adjustorThunk ? "AdjustorThunk_" : "")}{method.CreateCallSigName()}({paramListStr})
{{
    {(adjustorThunk ? "__arg0 += sizeof(Il2CppObject);" : "")}
{GenerateArgumentSizeAndOffset(method.ParamInfos)}
    StackObject args[__TOTAL_ARG_SIZE__];
{GenerateCopyArgumentToInterpreterStack(method.ParamInfos)}
    {(method.ReturnInfo.IsVoid ? "Interpreter::Execute(method, args, nullptr);" : $"{method.ReturnInfo.Type.GetTypeName()} ret; Interpreter::Execute(method, args, &ret); return ret;")}
}}
");
        }

        public void GenerateNative2ManagedMethod(MethodDesc method, List<string> lines)
        {
            GenerateNative2ManagedMethod0(method, false, lines);
        }

        public void GenerateAdjustThunkMethod(MethodDesc method, List<string> lines)
        {
            GenerateNative2ManagedMethod0(method, true, lines);
        }
    }
}
