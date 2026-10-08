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
using dnlib.DotNet.Writer;
using System.IO;

namespace HybridCLR.CodeGen.AOT
{
    public class AOTAssemblyMetadataStripper
    {
        public static byte[] Strip(byte[] assemblyBytes)
        {
            var context = ModuleDef.CreateModuleContext();
            var readerOption = new ModuleCreationOptions(context)
            {
                Runtime = CLRRuntimeReaderKind.Mono
            };
            var mod = ModuleDefMD.Load(assemblyBytes, readerOption);
            // remove all resources
            mod.Resources.Clear();
            foreach (var type in mod.GetTypes())
            {
                if (type.HasGenericParameters)
                {
                    continue;
                }
                foreach (var method in type.Methods)
                {
                    if (!method.HasBody || method.HasGenericParameters)
                    {
                        continue;
                    }
                    method.Body = null;
                }
            }
            var writer = new System.IO.MemoryStream();
            var options = new ModuleWriterOptions(mod);
            options.MetadataOptions.Flags |= MetadataFlags.PreserveRids;
            mod.Write(writer, options);
            writer.Flush();
            return writer.ToArray();
        }

        public static void Strip(string originalAssemblyPath, string strippedAssemblyPath)
        {
            byte[] originDllBytes = System.IO.File.ReadAllBytes(originalAssemblyPath);
            byte[] strippedDllBytes = Strip(originDllBytes);
            UnityEngine.Debug.Log($"aot dll:{originalAssemblyPath}, length: {originDllBytes.Length} -> {strippedDllBytes.Length}, stripping rate:{(originDllBytes.Length - strippedDllBytes.Length) / (double)originDllBytes.Length} ");
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(strippedAssemblyPath));
            System.IO.File.WriteAllBytes(strippedAssemblyPath, strippedDllBytes);
        }
    }
}
