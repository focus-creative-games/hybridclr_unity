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

﻿
using HybridCLR.CppCodeGen;
using HybridCLR.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace HybridCLR.CodeGen.AOT
{
    public class AOTGenericReferenceWriter
    {
        private static readonly Dictionary<Type, string> _typeNameMapping = new Dictionary<Type, string>
        {
            {typeof(bool), "bool" },
            {typeof(byte), "byte" },
            {typeof(sbyte), "sbyte" },
            {typeof(short), "short" },
            {typeof(ushort), "ushort" },
            {typeof(int), "int" },
            {typeof(uint), "uint" },
            {typeof(long), "long" },
            {typeof(ulong), "ulong" },
            {typeof(float), "float" },
            {typeof(double), "double" },
            {typeof(object), "object" },
            {typeof(string), "string" },
        };

        private readonly Dictionary<string, string> _typeSimpleNameMapping = new Dictionary<string, string>();
        private readonly Regex _systemTypePattern;
        private readonly Regex _genericPattern = new Regex(@"`\d+");

        public AOTGenericReferenceWriter()
        {
            foreach (var e in _typeNameMapping)
            {
                _typeSimpleNameMapping.Add(e.Key.FullName, e.Value);
            }
            _systemTypePattern = new Regex(string.Join("|", _typeSimpleNameMapping.Keys.Select(k => $@"\b{Regex.Escape(k)}\b")));
        }

        public string PrettifyTypeSig(string typeSig)
        {
            string s = _genericPattern.Replace(typeSig, "").Replace('/', '.');
            return _systemTypePattern.Replace(s, m => _typeSimpleNameMapping[m.Groups[0].Value]);
        }

        public string PrettifyMethodSig(string methodSig)
        {
            string s = PrettifyTypeSig(methodSig).Replace("::", ".");
            if (s.Contains(".ctor("))
            {
                s = "new " + s.Replace(".ctor(", "(");
            }
            return s;
        }

        public void Write(List<GenericClass> types, List<GenericMethod> methods, string outputFile)
        {
            string parentDir = Directory.GetParent(outputFile).FullName;
            Directory.CreateDirectory(parentDir);

            var cw = new CodeWriter();
            cw.WriteLine("using System.Collections.Generic;");
            cw.WriteLine("public class AOTGenericReferences : UnityEngine.MonoBehaviour");
            cw.WriteLine("{");
            cw.IncreaseIndent();

            cw.WriteLine();
            cw.WriteLine("// {{ AOT assemblies");
            cw.WriteLine("public static readonly IReadOnlyList<string> PatchedAOTAssemblyList = new List<string>");
            cw.WriteLine("{");
            cw.IncreaseIndent();
            List<dnlib.DotNet.ModuleDef> modules = new HashSet<dnlib.DotNet.ModuleDef>(
                types.Select(t => t.Type.Module).Concat(methods.Select(m => m.Method.Module))).ToList();
            modules.Sort((a, b) => a.Name.CompareTo(b.Name));
            foreach (dnlib.DotNet.ModuleDef module in modules)
            {
                cw.WriteLine($"\"{module.Name}\",");
            }
            cw.DecreaseIndent();
            cw.WriteLine("};");
            cw.WriteLine("// }}");

            cw.WriteLine();
            cw.WriteLine("// {{ constraint implement type");
            cw.WriteLine("// }} ");

            cw.WriteLine();
            cw.WriteLine("// {{ AOT generic types");

            List<string> typeNames = types.Select(t => PrettifyTypeSig(t.ToTypeSig().ToString())).ToList();
            typeNames.Sort(string.CompareOrdinal);
            foreach (var typeName in typeNames)
            {
                cw.WriteLine($"// {typeName}");
            }

            cw.WriteLine("// }}");

            cw.WriteLine();
            cw.WriteLine("public void RefMethods()");
            cw.WriteLine("{");
            cw.IncreaseIndent();

            List<(string, string, string)> methodTypeAndNames = methods.Select(m =>
                (PrettifyTypeSig(m.Method.DeclaringType.ToString()), PrettifyMethodSig(m.Method.Name), PrettifyMethodSig(m.ToMethodSpec().ToString())))
                .ToList();
            methodTypeAndNames.Sort((a, b) =>
            {
                int c = String.Compare(a.Item1, b.Item1, StringComparison.Ordinal);
                if (c != 0)
                {
                    return c;
                }

                c = String.Compare(a.Item2, b.Item2, StringComparison.Ordinal);
                if (c != 0)
                {
                    return c;
                }
                return String.Compare(a.Item3, b.Item3, StringComparison.Ordinal);
            });
            foreach (var method in methodTypeAndNames)
            {
                cw.WriteLine($"// {PrettifyMethodSig(method.Item3)}");
            }
            cw.DecreaseIndent();
            cw.WriteLine("}");

            cw.DecreaseIndent();
            cw.WriteLine("}");

            cw.Save(outputFile);
        }
    }
}
