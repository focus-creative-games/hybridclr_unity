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
using HybridCLR.Mod;
using System.Collections.Generic;
using System.IO;

namespace HybridCLR.HotUpdate
{
    public class MissingMetadataChecker
    {
        private readonly HashSet<string> _aotAssNames;

        private readonly HashSet<string> _hotUpdateAssNames;

        private readonly AssemblyCache _assCache;

        public MissingMetadataChecker(string aotDllDir, IEnumerable<string> hotUpdateAssNames)
        {

            _hotUpdateAssNames = new HashSet<string>(hotUpdateAssNames ?? new List<string>());
            _aotAssNames = new HashSet<string>();
            foreach (var aotFile in Directory.GetFiles(aotDllDir, "*.dll"))
            {
                string aotAssName = Path.GetFileNameWithoutExtension(aotFile);
                if (_hotUpdateAssNames.Contains(aotAssName))
                {
                    continue;
                }
                _aotAssNames.Add(aotAssName);
            }
            _assCache = new AssemblyCache(new PathAssemblyResolver(aotDllDir));
        }

        public bool Check(string hotUpdateDllPath)
        {
            bool anyMissing = false;

            ModuleDef mod = ModuleDefMD.Load(File.ReadAllBytes(hotUpdateDllPath), _assCache.ModCtx);

            foreach (var refass in mod.GetAssemblyRefs())
            {
                string refAssName = refass.Name;
                if (_aotAssNames.Contains(refAssName))
                {
                    _assCache.LoadModule(refass.Name, true);
                }
                else if (!_hotUpdateAssNames.Contains(refAssName))
                {
                    UnityEngine.Debug.LogError($"Missing AOT Assembly: {refAssName}");
                    anyMissing = true;
                }
            }


            foreach (TypeRef typeRef in mod.GetTypeRefs())
            {
                string defAssName = typeRef.DefinitionAssembly.Name;
                if (!_aotAssNames.Contains(defAssName))
                {
                    continue;
                }
                if (typeRef.ResolveTypeDef() == null)
                {
                    UnityEngine.Debug.LogError($"Missing Type: {typeRef.FullName}");
                    anyMissing = true;
                }
            }

            foreach (IMethodDefOrRef memberRef in mod.GetMemberRefs())
            {
                if (memberRef.DeclaringType.DefinitionAssembly == null)
                {
                    continue;
                }
                string defAssName = memberRef.DeclaringType.DefinitionAssembly.Name;
                if (!_aotAssNames.Contains(defAssName))
                {
                    continue;
                }
                if (memberRef.IsField)
                {
                    IField field = (IField)memberRef;
                    if (field.ResolveFieldDef() == null)
                    {
                        UnityEngine.Debug.LogError($"Missing Field: {memberRef.FullName}");
                        anyMissing = true;
                    }
                }
                else if (memberRef.IsMethod)
                {
                    TypeSig declaringTypeSig = memberRef.DeclaringType.ToTypeSig();
                    if (memberRef.ResolveMethodDef() == null)
                    {
                        if (declaringTypeSig.ElementType == ElementType.Array || declaringTypeSig.ElementType == ElementType.SZArray)
                        {
                            continue;
                        }
                        UnityEngine.Debug.LogError($"Missing Method: {memberRef.FullName}");
                        anyMissing = true;
                    }
                }
            }
            return !anyMissing;
        }
    }
}
