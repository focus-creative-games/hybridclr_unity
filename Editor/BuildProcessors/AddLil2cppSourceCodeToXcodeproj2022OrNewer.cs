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

#if UNITY_2022 && (UNITY_IOS || UNITY_TVOS || UNITY_VISIONOS)

namespace HybridCLR.BuildProcessors
{
    public static class AddLil2cppSourceCodeToXcodeproj2022OrNewer
    {

        [PostProcessBuild]
        public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (!HybridCLRSettings.Instance.enable)
                return;
            string pbxprojFile = BuildProcessorUtil.GetXcodeProjectFile(pathToBuiltProject);
            RemoveExternalLibil2cppOption(pbxprojFile);
            CopyLibil2cppToXcodeProj(pathToBuiltProject);
        }

        private static string TryRemoveDunplicateShellScriptSegment(string pbxprojFile, string pbxprojContent)
        {
            // will appear duplicated Shell Script segment when append to existed xcode project.
            // This is unity bug.
            // we remove duplicated Shell Script to avoid build error.
            string copyFileComment = @"/\* CopyFiles \*/,\s+([A-Z0-9]{24}) /\* ShellScript \*/,\s+([A-Z0-9]{24}) /\* ShellScript \*/,";
            var m = Regex.Match(pbxprojContent, copyFileComment, RegexOptions.Multiline);
            if (!m.Success)
            {
                return pbxprojContent;
            }

            if (m.Groups[1].Value != m.Groups[2].Value)
            {
                throw new BuildFailedException($"find invalid /* ShellScript */ segment");
            }

            int startIndexOfDupShellScript = m.Groups[2].Index;
            int endIndexOfDupShellScript = pbxprojContent.IndexOf(",", startIndexOfDupShellScript);

            pbxprojContent = pbxprojContent.Remove(startIndexOfDupShellScript, endIndexOfDupShellScript + 1 - startIndexOfDupShellScript);
            Debug.LogWarning($"[AddLil2cppSourceCodeToXcodeproj] remove duplicated '/* ShellScript */' from file '{pbxprojFile}'");
            return pbxprojContent;
        }

        private static void RemoveExternalLibil2cppOption(string pbxprojFile)
        {
            string pbxprojContent = File.ReadAllText(pbxprojFile, Encoding.UTF8);
            string removeBuildOption = @"--external-lib-il2-cpp=\""$PROJECT_DIR/Libraries/libil2cpp.a\""";
            if (pbxprojContent.Contains(removeBuildOption))
            {
                pbxprojContent = pbxprojContent.Replace(removeBuildOption, "");
                Debug.Log($"[AddLil2cppSourceCodeToXcodeproj] remove il2cpp build option '{removeBuildOption}' from file '{pbxprojFile}'");
            }
            else
            {
                Debug.LogWarning($"[AddLil2cppSourceCodeToXcodeproj] project.pbxproj remove building option:'{removeBuildOption}' fail. This may occur when 'Append' to existing xcode project in building");
            }

            pbxprojContent = TryRemoveDunplicateShellScriptSegment(pbxprojFile, pbxprojContent);


            File.WriteAllText(pbxprojFile, pbxprojContent, Encoding.UTF8);
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