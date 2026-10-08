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
using System;
using System.Collections.Generic;
using UnityEngine;
using IAssemblyResolver = HybridCLR.Mod.IAssemblyResolver;

namespace HybridCLR.CodeGen.Link
{
    public class LinkAnalyzer
    {
        private readonly IAssemblyResolver _resolver;

        public LinkAnalyzer(IAssemblyResolver resolver)
        {
            _resolver = resolver;
        }

        public List<LinkTypePreserveInfo> CollectRefs(List<string> rootAssemblies, LinkPreserveLevel preserveLevel)
        {
            var assCollector = new AssemblyCache(_resolver);
            var rootAssemblyNames = new HashSet<string>(rootAssemblies);
            var types = new Dictionary<(string Assembly, string Type), LinkTypePreserveInfo>();

            foreach (var rootAss in rootAssemblies)
            {
                var dnAss = assCollector.LoadModule(rootAss, false);
                CollectTypeRefs(dnAss, rootAssemblyNames, preserveLevel, types);
                if (preserveLevel == LinkPreserveLevel.PreserveExactMember)
                {
                    CollectMemberRefs(dnAss, rootAssemblyNames, types);
                }
            }

            var result = new List<LinkTypePreserveInfo>(types.Values);
            result.Sort((a, b) =>
            {
                int c = string.Compare(a.AssemblyName, b.AssemblyName, StringComparison.Ordinal);
                if (c != 0)
                {
                    return c;
                }
                return string.Compare(a.TypeFullName, b.TypeFullName, StringComparison.Ordinal);
            });
            return result;
        }

        private static void CollectTypeRefs(ModuleDefMD dnAss, HashSet<string> rootAssemblyNames,
            LinkPreserveLevel preserveLevel, Dictionary<(string, string), LinkTypePreserveInfo> types)
        {
            foreach (var type in dnAss.GetTypeRefs())
            {
                if (type.DefinitionAssembly == null)
                {
                    Debug.LogWarning($"assembly:{dnAss.Name} TypeRef {type.FullName} has no DefinitionAssembly");
                    continue;
                }
                string assName = type.DefinitionAssembly.Name.ToString();
                if (rootAssemblyNames.Contains(assName))
                {
                    continue;
                }

                var info = GetOrCreateTypeInfo(types, assName, type.FullName);
                if (preserveLevel == LinkPreserveLevel.PreserveAllOfDeclaringType)
                {
                    info.PreserveAll = true;
                }
            }
        }

        private static void CollectMemberRefs(ModuleDefMD dnAss, HashSet<string> rootAssemblyNames,
            Dictionary<(string, string), LinkTypePreserveInfo> types)
        {
            foreach (IMethodDefOrRef memberRef in dnAss.GetMemberRefs())
            {
                ITypeDefOrRef declaringType = memberRef.DeclaringType;
                if (declaringType?.DefinitionAssembly == null)
                {
                    continue;
                }

                string assName = declaringType.DefinitionAssembly.Name.ToString();
                if (rootAssemblyNames.Contains(assName))
                {
                    continue;
                }

                TypeSig declaringTypeSig = declaringType.ToTypeSig();
                if (declaringTypeSig != null &&
                    (declaringTypeSig.ElementType == ElementType.Array || declaringTypeSig.ElementType == ElementType.SZArray))
                {
                    continue;
                }

                string typeFullName = GetLinkTypeFullName(declaringType);
                if (string.IsNullOrEmpty(typeFullName))
                {
                    continue;
                }

                var info = GetOrCreateTypeInfo(types, assName, typeFullName);
                string memberName = memberRef.Name;

                if (memberRef.IsField)
                {
                    info.Fields.Add(memberName);
                    continue;
                }

                if (!memberRef.IsMethod)
                {
                    continue;
                }

                if (TryGetPropertyName(memberName, out string propertyName))
                {
                    info.Properties.Add(propertyName);
                    continue;
                }

                if (TryGetEventName(memberName, out string eventName))
                {
                    info.Events.Add(eventName);
                    continue;
                }

                info.Methods.Add(memberName);
            }
        }

        private static LinkTypePreserveInfo GetOrCreateTypeInfo(
            Dictionary<(string, string), LinkTypePreserveInfo> types, string assemblyName, string typeFullName)
        {
            var key = (assemblyName, typeFullName);
            if (!types.TryGetValue(key, out var info))
            {
                info = new LinkTypePreserveInfo
                {
                    AssemblyName = assemblyName,
                    TypeFullName = typeFullName,
                };
                types.Add(key, info);
            }
            return info;
        }

        private static string GetLinkTypeFullName(ITypeDefOrRef type)
        {
            if (type is TypeSpec typeSpec)
            {
                if (typeSpec.TypeSig is GenericInstSig genericInst)
                {
                    return genericInst.GenericType.FullName;
                }
                return typeSpec.FullName;
            }
            return type.FullName;
        }

        private static bool TryGetPropertyName(string methodName, out string propertyName)
        {
            propertyName = null;
            // Skip explicit interface implementations (contain '.').
            if (methodName.IndexOf('.') >= 0)
            {
                return false;
            }
            if (methodName.StartsWith("get_", StringComparison.Ordinal) && methodName.Length > 4)
            {
                propertyName = methodName.Substring(4);
                return true;
            }
            if (methodName.StartsWith("set_", StringComparison.Ordinal) && methodName.Length > 4)
            {
                propertyName = methodName.Substring(4);
                return true;
            }
            return false;
        }

        private static bool TryGetEventName(string methodName, out string eventName)
        {
            eventName = null;
            if (methodName.IndexOf('.') >= 0)
            {
                return false;
            }
            if (methodName.StartsWith("add_", StringComparison.Ordinal) && methodName.Length > 4)
            {
                eventName = methodName.Substring(4);
                return true;
            }
            if (methodName.StartsWith("remove_", StringComparison.Ordinal) && methodName.Length > 7)
            {
                eventName = methodName.Substring(7);
                return true;
            }
            return false;
        }
    }
}
