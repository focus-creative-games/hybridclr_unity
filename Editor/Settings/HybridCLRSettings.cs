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

using HybridCLR.CodeGen.Link;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace HybridCLR.Settings
{

    [FilePath("ProjectSettings/HybridCLRSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class HybridCLRSettings : ScriptableSingleton<HybridCLRSettings>
    {

        [Tooltip("enable HybridCLR")]
        public bool enable = true;

        [Tooltip("use il2cpp in unity editor installation location")]
        public bool useGlobalIl2cpp;

        [Tooltip("hybridclr repo URL")]
        public string hybridclrRepoURL = "https://gitee.com/focus-creative-games/hybridclr";

        [Tooltip("il2cpp_plus repo URL")]
        public string il2cppPlusRepoURL = "https://gitee.com/focus-creative-games/il2cpp_plus";

        [Tooltip("hot update assembly definitions(asd)")]
        public AssemblyDefinitionAsset[] hotUpdateAssemblyDefinitions;

        [Tooltip("hot update assembly names(without .dll suffix)")]
        public string[] hotUpdateAssemblies;

        [Tooltip("preserved hot update assembly names(without .dll suffix)")]
        public string[] preserveHotUpdateAssemblies;

        [Tooltip("searching paths of external hot update assemblies")]
        public string[] externalHotUpdateAssembliyDirs;

        [Tooltip("preserve level when generating link.xml. PreserveAllOfDeclaringType keeps whole referenced types (safer, larger). PreserveExactMember keeps only referenced fields/methods/properties/events (smaller).")]
        public LinkPreserveLevel linkPreserveLevel = LinkPreserveLevel.PreserveAllOfDeclaringType;

        [Tooltip("output file of automatic generated link.xml by scanning hot update assemblies")]
        public string outputLinkFile = "HybridCLR/Generate/link.xml";

        [Tooltip("output file of automatic generated AOTGenericReferences.cs")]
        public string outputAOTGenericReferenceFile = "HybridCLR/Generate/AOTGenericReferences.cs";

        [Tooltip("max iteration count of searching generic methods in hot update assemblies")]
        public int maxGenericReferenceIteration = 10;

        [Tooltip("max iteration count of searching method bridge generic methods in AOT assemblies")]
        public int maxMethodBridgeGenericIteration = 10;

        public static void Save()
        {
            if (!instance)
            {
                return;
            }

            instance.Save(true);
        }
    }
}
