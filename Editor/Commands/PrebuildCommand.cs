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

﻿using UnityEditor;
using UnityEditor.Build;

namespace HybridCLR.Commands
{
    public static class PrebuildCommand
    {
        /// <summary>
        /// Execute all generation operations in the necessary order, suitable for pre-build operations
        /// </summary>
        [MenuItem("HybridCLR/Generate/All", priority = 200)]
        public static void GenerateAll()
        {
            var installer = new Installer.InstallerController();
            if (!installer.HasInstalledHybridCLR())
            {
                throw new BuildFailedException($"You have not initialized HybridCLR, please install it via menu 'HybridCLR/Installer'");
            }
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            CompileDllCommand.CompileDll(target, EditorUserBuildSettings.development);
            Il2CppDefGeneratorCommand.GenerateIl2CppDef();

            // These generation depend on HotUpdateDlls
            LinkGeneratorCommand.GenerateLinkXml(target);

            // Generate stripped aot dll
            StripAOTDllCommand.GenerateStripedAOTDlls(target);

            // Method bridge generation depends on AOT dll, must be built before generating AOT dll
            MethodBridgeGeneratorCommand.GenerateMethodBridgeAndReversePInvokeWrapper(target);
            AOTReferenceGeneratorCommand.GenerateAOTGenericReference(target);
        }
    }
}
