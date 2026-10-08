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

#if UNITY_2023_1_OR_NEWER && (UNITY_IOS || UNITY_TVOS || UNITY_VISIONOS)

namespace HybridCLR.BuildProcessors
{
    public static class AddLil2cppSourceCodeToXcodeproj2023OrNewer
    {

        [PostProcessBuild]
        public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (!HybridCLRSettings.Instance.enable)
                return;
            CopyLibil2cppToXcodeProj(pathToBuiltProject);
        }

        private static void CopyLibil2cppToXcodeProj(string pathToBuiltProject)
        {
            string srcLibil2cppDir = $"{SettingsUtil.LocalIl2CppDir}/libil2cpp";
            string destLibil2cppDir = $"{pathToBuiltProject}/Il2CppOutputProject/IL2CPP/libil2cpp";
            BashUtil.RemoveDir(destLibil2cppDir);
            BashUtil.CopyDir(srcLibil2cppDir, destLibil2cppDir, true);
        }
    }
}
#endif