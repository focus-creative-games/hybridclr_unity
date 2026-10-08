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

﻿using HybridCLR.CppCodeGen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HybridCLR.CodeGen.Link
{
    public class LinkXmlWriter
    {
        public void Write(string outputLinkXmlFile, List<LinkTypePreserveInfo> preserveTypes)
        {
            string parentDir = Directory.GetParent(outputLinkXmlFile).FullName;
            Directory.CreateDirectory(parentDir);

            var cw = new CodeWriter(2);
            cw.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            cw.WriteLine("<linker>");
            cw.IncreaseIndent();

            var typesByAssembly = preserveTypes.GroupBy(t => t.AssemblyName).ToList();
            typesByAssembly.Sort((a, b) => String.Compare(a.Key, b.Key, StringComparison.Ordinal));

            foreach (var assembly in typesByAssembly)
            {
                cw.WriteLine($"<assembly fullname=\"{assembly.Key}\">");
                cw.IncreaseIndent();

                List<LinkTypePreserveInfo> assTypes = assembly.ToList();
                assTypes.Sort((a, b) => string.CompareOrdinal(a.TypeFullName, b.TypeFullName));
                foreach (var typeInfo in assTypes)
                {
#if UNITY_2023_1_OR_NEWER
                    // if preserve all of 'UnityEngine.Debug' type, unity will build failed on iOS platform because missing some icalls.
                    // this is a known issue of Unity.
                    if (typeInfo.TypeFullName == "UnityEngine.Debug")
                    {
                        continue;
                    }
#endif
                    WriteType(cw, typeInfo);
                }

                cw.DecreaseIndent();
                cw.WriteLine("</assembly>");
            }

            cw.DecreaseIndent();
            cw.WriteLine("</linker>");
            cw.Save(outputLinkXmlFile);
        }

        private static void WriteType(CodeWriter cw, LinkTypePreserveInfo typeInfo)
        {
            if (typeInfo.PreserveAll)
            {
                cw.WriteLine($"<type fullname=\"{typeInfo.TypeFullName}\" preserve=\"all\" />");
                return;
            }

            if (!typeInfo.HasExactMembers)
            {
                cw.WriteLine($"<type fullname=\"{typeInfo.TypeFullName}\" />");
                return;
            }

            cw.WriteLine($"<type fullname=\"{typeInfo.TypeFullName}\">");
            cw.IncreaseIndent();
            foreach (var field in typeInfo.Fields)
            {
                cw.WriteLine($"<field name=\"{field}\" />");
            }
            foreach (var method in typeInfo.Methods)
            {
                cw.WriteLine($"<method name=\"{method}\" />");
            }
            foreach (var property in typeInfo.Properties)
            {
                cw.WriteLine($"<property name=\"{property}\" />");
            }
            foreach (var evt in typeInfo.Events)
            {
                cw.WriteLine($"<event name=\"{evt}\" />");
            }
            cw.DecreaseIndent();
            cw.WriteLine("</type>");
        }
    }
}
