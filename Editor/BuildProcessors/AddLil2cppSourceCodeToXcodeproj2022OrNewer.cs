using HybridCLR.Editor.Installer;
using HybridCLR.Editor.Settings;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using UnityEngine;

#if UNITY_2022 && (UNITY_IOS || UNITY_TVOS || UNITY_VISIONOS)

namespace HybridCLR.Editor.BuildProcessors
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

            int strShellScriptIndex1 = pbxprojContent.IndexOf("/* ShellScript */,");
            int strShellScriptIndex2 = pbxprojContent.IndexOf("/* ShellScript */,", strShellScriptIndex1 + 10);
            if (strShellScriptIndex2 >= 0)
            {
                pbxprojContent = pbxprojContent.Remove(strShellScriptIndex1, strShellScriptIndex2 - strShellScriptIndex1);
                Debug.LogWarning($"[AddLil2cppSourceCodeToXcodeproj] remove duplicated '/* ShellScript */' from file '{pbxprojFile}'");
            }

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