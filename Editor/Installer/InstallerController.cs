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

#if UNITY_6000_3_OR_NEWER && UNITY_EDITOR_OSX
#define NEW_IL2CPP_PATH
#endif
using HybridCLR.Settings;
using HybridCLR.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace HybridCLR.Installer
{

    public class InstallerController
    {
        private const string hybridclr_repo_path = "hybridclr_repo";

        private const string il2cpp_plus_repo_path = "il2cpp_plus_repo";

        public int MajorVersion => _curVersion.major;

        private readonly UnityVersion _curVersion;

        private readonly HybridclrVersionInfo _curDefaultVersion;

        public string PackageVersion { get; private set; }

        public string InstalledLibil2cppVersion { get; private set; }

        public InstallerController()
        {
            _curVersion = UnityVersion.ParseUnityVersion(Application.unityVersion);
            HybridclrVersionManifest versionManifest = GetHybridCLRVersionManifest();

            _curDefaultVersion = FindMatchedVersion(versionManifest.versions, _curVersion);
            PackageVersion = LoadPackageInfo().version;
            InstalledLibil2cppVersion = ReadLocalVersion();
        }

        private HybridclrVersionManifest GetHybridCLRVersionManifest()
        {
            string versionFile = $"{SettingsUtil.ProjectDir}/{SettingsUtil.HybridCLRDataPathInPackage}/hybridclr_version.json";
            return JsonUtility.FromJson<HybridclrVersionManifest>(File.ReadAllText(versionFile, Encoding.UTF8));
        }

        private PackageInfo LoadPackageInfo()
        {
            string packageJson = $"{SettingsUtil.ProjectDir}/Packages/{SettingsUtil.PackageName}/package.json";
            return JsonUtility.FromJson<PackageInfo>(File.ReadAllText(packageJson, Encoding.UTF8));
        }

        private HybridclrVersionInfo FindMatchedVersion(List<HybridclrVersionInfo> versions, UnityVersion curVer)
        {
            HybridclrVersionInfo bestMatch = null;
            foreach (var v in versions)
            {
                string[] versionWithEngineParts = v.unity_version.Split('-');
                bool isTuanjieVersion = versionWithEngineParts.Length >= 2 && versionWithEngineParts[1] == "tuanjie";
                if (curVer.isTuanjieEngine != isTuanjieVersion)
                {
                    continue;
                }

                string[] versionParts = versionWithEngineParts[0].Split('.');
                if (int.Parse(versionParts[0]) == curVer.major && (versionParts.Length < 2 || int.Parse(versionParts[1]) == curVer.minor1))
                {
                    bestMatch = v;
                }
            }
            if (bestMatch == null)
            {
                string compatibleVersions = string.Join(", ", versions.Select(v => v.unity_version));
                throw new NotSupportedException($"No compatible version found for Unity {curVer.major}.{curVer.minor1}.{curVer.minor2}, please use compatible version of Unity: {compatibleVersions}");
            }
            UnityEngine.Debug.Log($"bestMatch: {bestMatch.unity_version} {bestMatch.il2cpp_plus.tag} {bestMatch.hybridclr.tag} ");
            return bestMatch;
        }

        [Serializable]
        class PackageInfo
        {
            public string name;

            public string version;
        }

        [Serializable]
        class VersionDesc
        {
            public string tag;

            //public string hash;
        }

        [Serializable]
        class HybridclrVersionInfo
        {
            public string unity_version;

            public VersionDesc hybridclr;

            public VersionDesc il2cpp_plus;
        }

        [Serializable]
        class HybridclrVersionManifest
        {
            public List<HybridclrVersionInfo> versions;
        }


        public string HybridclrLocalVersion => _curDefaultVersion?.hybridclr?.tag;

        public string Il2cppPlusLocalVersion => _curDefaultVersion?.il2cpp_plus?.tag;

        public string ApplicationIl2cppPath
        {
            get
            {
                Debug.Log($"application path:{EditorApplication.applicationPath} {EditorApplication.applicationContentsPath}");
#if NEW_IL2CPP_PATH
#if UNITY_IOS
                string platformDirName = "iOSSupport";
#elif UNITY_TVOS
                string platformDirName = "AppleTVSupport";
#elif UNITY_VISIONOS
                string platformDirName = "VisionOSPlayer";
#else
                string platformDirName = "iOSSupport";
#endif
                return $"{EditorApplication.applicationContentsPath}/../../PlaybackEngines/{platformDirName}/il2cpp";
#else
                return $"{EditorApplication.applicationContentsPath}/il2cpp";
#endif
            }
        }

        public string LocalVersionFile => $"{SettingsUtil.LocalIl2CppDir}/libil2cpp/hybridclr/generated/libil2cpp-version.txt";

        private string ReadLocalVersion()
        {
            if (!File.Exists(LocalVersionFile))
            {
                return null;
            }
            return File.ReadAllText(LocalVersionFile, Encoding.UTF8);
        }

        public void WriteLocalVersion()
        {
            InstalledLibil2cppVersion = PackageVersion;
            Directory.CreateDirectory(Path.GetDirectoryName(LocalVersionFile));
            File.WriteAllText(LocalVersionFile, PackageVersion, Encoding.UTF8);
            Debug.Log($"Write installed version:'{PackageVersion}' to {LocalVersionFile}");
        }

        public void InstallDefaultHybridCLR()
        {
            InstallFromLocal(PrepareLibil2cppWithHybridclrFromGitRepo());
        }

        public bool HasInstalledHybridCLR()
        {
            return Directory.Exists($"{SettingsUtil.LocalIl2CppDir}/libil2cpp/hybridclr");
        }

        void CloneBranch(string workDir, string repoUrl, string branch, string repoDir)
        {
            DirectoryUtil.RemoveDir(repoDir);
            BashUtil.RunCommand(workDir, "git", new string[] { "clone", "-b", branch, "--depth", "1", repoUrl, repoDir });
        }

        private string PrepareLibil2cppWithHybridclrFromGitRepo()
        {
            string workDir = SettingsUtil.HybridCLRDataDir;
            Directory.CreateDirectory(workDir);
            //BashUtil.RecreateDir(workDir);

            // clone hybridclr
            string hybridclrRepoURL = HybridCLRSettings.instance.hybridclrRepoURL;
            string hybridclrRepoDir = $"{workDir}/{hybridclr_repo_path}";
            CloneBranch(workDir, hybridclrRepoURL, _curDefaultVersion.hybridclr.tag, hybridclrRepoDir);

            if (!Directory.Exists(hybridclrRepoDir))
            {
                throw new Exception($"clone hybridclr fail. url: {hybridclrRepoURL}");
            }

            // clone il2cpp_plus
            string il2cppPlusRepoURL = HybridCLRSettings.instance.il2cppPlusRepoURL;
            string il2cppPlusRepoDir = $"{workDir}/{il2cpp_plus_repo_path}";
            CloneBranch(workDir, il2cppPlusRepoURL, _curDefaultVersion.il2cpp_plus.tag, il2cppPlusRepoDir);

            if (!Directory.Exists(il2cppPlusRepoDir))
            {
                throw new Exception($"clone il2cpp_plus fail. url: {il2cppPlusRepoDir}");
            }

            Directory.Move($"{hybridclrRepoDir}/hybridclr", $"{il2cppPlusRepoDir}/libil2cpp/hybridclr");
            return $"{il2cppPlusRepoDir}/libil2cpp";
        }

        public void InstallFromLocal(string libil2cppWithHybridclrSourceDir)
        {
            RunInitLocalIl2CppData(ApplicationIl2cppPath, libil2cppWithHybridclrSourceDir);
        }

        private void RunInitLocalIl2CppData(string editorIl2cppPath, string libil2cppWithHybridclrSourceDir)
        {
            string workDir = SettingsUtil.HybridCLRDataDir;
            Directory.CreateDirectory(workDir);

            // create LocalIl2Cpp
            string localUnityDataDir = SettingsUtil.LocalUnityDataDir;
            DirectoryUtil.RecreateDir(localUnityDataDir);
#if !NEW_IL2CPP_PATH
            // copy MonoBleedingEdge
            DirectoryUtil.CopyDir($"{Directory.GetParent(editorIl2cppPath)}/MonoBleedingEdge", $"{localUnityDataDir}/MonoBleedingEdge", true);
#endif
            // copy il2cpp
            DirectoryUtil.CopyDir(editorIl2cppPath, SettingsUtil.LocalIl2CppDir, true);
#if NEW_IL2CPP_PATH
            string buildDir = $"{SettingsUtil.LocalIl2CppDir}/build";
            if (RuntimeInformation.ProcessArchitecture == Architecture.Arm || RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            {
                DirectoryUtil.CopyDir($"{buildDir}/deploy_arm64", $"{buildDir}/deploy", false);
            }
            else
            {
                DirectoryUtil.CopyDir($"{buildDir}/deploy_x86_64", $"{buildDir}/deploy", false);
            }
#endif

            // replace libil2cpp
            string dstLibil2cppDir = $"{SettingsUtil.LocalIl2CppDir}/libil2cpp";
            DirectoryUtil.CopyDir($"{libil2cppWithHybridclrSourceDir}", dstLibil2cppDir, true);

            // clean Il2cppBuildCache
            DirectoryUtil.RemoveDir($"{SettingsUtil.ProjectDir}/Library/Il2cppBuildCache", true);
            if (HasInstalledHybridCLR())
            {
                WriteLocalVersion();
                Debug.Log("Install Sucessfully");
            }
            else
            {
                Debug.LogError("Installation failed!");
            }
        }
    }
}
