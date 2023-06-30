using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using System.Text.RegularExpressions;
using System.Linq;

namespace HybridCLR.Editor.Installer
{

    public class InstallerController
    {
        private const string hybridclr_repo_path = "hybridclr_repo";

        private const string il2cpp_plus_repo_path = "il2cpp_plus_repo";

        public int MajorVersion => _curVersion.major;

        private readonly UnityVersion _curVersion;

        private readonly HybridclrVersionManifest _versionManifest;
        private readonly HybridclrVersionInfo _curDefaultVersion;

        public string PackageVersion { get; private set; }

        public InstallerController()
        {
            _curVersion = ParseUnityVersion(Application.unityVersion);
            _versionManifest = GetHybridCLRVersionManifest();
            _curDefaultVersion = _versionManifest.versions.FirstOrDefault(v => v.unity_version == _curVersion.major.ToString());
            PackageVersion = LoadPackageInfo().version;
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


        [Serializable]
        class PackageInfo
        {
            public string name;

            public string version;
        }

        [Serializable]
        class VersionDesc
        {
            public string branch;

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

        private class UnityVersion
        {
            public int major;
            public int minor1;
            public int minor2;

            public override string ToString()
            {
                return $"{major}.{minor1}.{minor2}";
            }
        }

        private static readonly Regex s_unityVersionPat = new Regex(@"(\d+)\.(\d+)\.(\d+)");

        public const int min2020_3_CompatibleMinorVersion = 26;
        public const int min2021_3_CompatibleMinorVersion = 0;
        public const int min2022_3_CompatibleMinorVersion = 0;

        private UnityVersion ParseUnityVersion(string versionStr)
        {
            var matches = s_unityVersionPat.Matches(versionStr);
            if (matches.Count == 0)
            {
                return null;
            }
            // 找最后一个匹配的
            Match match = matches[matches.Count - 1];
            // Debug.Log($"capture count:{match.Groups.Count} {match.Groups[1].Value} {match.Groups[2].Value}");
            int major = int.Parse(match.Groups[1].Value);
            int minor1 = int.Parse(match.Groups[2].Value);
            int minor2 = int.Parse(match.Groups[3].Value);
            return new UnityVersion { major = major, minor1 = minor1, minor2 = minor2 };
        }

        public string GetCurrentUnityVersionMinCompatibleVersionStr()
        {
            return GetMinCompatibleVersion(MajorVersion);
        }

        public string GetMinCompatibleVersion(int majorVersion)
        {
            switch(majorVersion)
            {
                case 2020: return $"2020.3.{min2020_3_CompatibleMinorVersion}";
                case 2021: return $"2021.3.{min2021_3_CompatibleMinorVersion}";
                case 2022: return $"2022.3.{min2022_3_CompatibleMinorVersion}";
                default: return $"2020.3.{min2020_3_CompatibleMinorVersion}";
            }
        }

        public bool IsCompatibleVersion()
        {
            UnityVersion version = _curVersion;
            if (version == null)
            {
                return false;
            }
            if (version.minor1 != 3)
            {
                return false;
            }
            switch (version.major)
            {
                case 2020:
                    {
                        return version.minor2 >= min2021_3_CompatibleMinorVersion;
                    }
                case 2021:
                    { 
                        return version.minor2 >= min2021_3_CompatibleMinorVersion;
                    }
                case 2022:
                    {
                        return version.minor2 >= min2022_3_CompatibleMinorVersion;
                    }
                default: throw new Exception($"not support il2cpp_plus branch:{version.major}");
            }
        }

        public string HybridclrLocalVersion => _curDefaultVersion?.hybridclr?.branch;

        public string Il2cppPlusLocalVersion => _curDefaultVersion?.il2cpp_plus?.branch;


        private string GetIl2CppPathByContentPath(string contentPath)
        {
            return $"{contentPath}/il2cpp";
        }

        public string ApplicationIl2cppPath => GetIl2CppPathByContentPath(EditorApplication.applicationContentsPath);

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
            BashUtil.RemoveDir(repoDir);
            BashUtil.RunCommand(workDir, "git", new string[] {"clone", "-b", branch, "--depth", "1", repoUrl, repoDir});
        }

        private string PrepareLibil2cppWithHybridclrFromGitRepo()
        {
            string workDir = SettingsUtil.HybridCLRDataDir;
            Directory.CreateDirectory(workDir);
            //BashUtil.RecreateDir(workDir);

            // clone hybridclr
            string hybridclrRepoURL = HybridCLRSettings.Instance.hybridclrRepoURL;
            string hybridclrRepoDir = $"{workDir}/{hybridclr_repo_path}";
            CloneBranch(workDir, hybridclrRepoURL, _curDefaultVersion.hybridclr.branch, hybridclrRepoDir);

            if (!Directory.Exists(hybridclrRepoDir))
            {
                throw new Exception($"clone hybridclr fail. url: {hybridclrRepoURL}");
            }

            // clone il2cpp_plus
            string il2cppPlusRepoURL = HybridCLRSettings.Instance.il2cppPlusRepoURL;
            string il2cppPlusRepoDir = $"{workDir}/{il2cpp_plus_repo_path}";
            CloneBranch(workDir, il2cppPlusRepoURL, _curDefaultVersion.il2cpp_plus.branch, il2cppPlusRepoDir);

            if (!Directory.Exists(il2cppPlusRepoDir))
            {
                throw new Exception($"clone il2cpp_plus fail. url: {il2cppPlusRepoDir}");
            }

            Directory.Move($"{hybridclrRepoDir}/hybridclr", $"{il2cppPlusRepoDir}/libil2cpp/hybridclr");
            return $"{il2cppPlusRepoDir}/libil2cpp";
        }

        public void InstallFromLocal(string libil2cppWithHybridclrSourceDir)
        {
            RunInitLocalIl2CppData(ApplicationIl2cppPath, libil2cppWithHybridclrSourceDir, _curVersion);
        }

        private void RunInitLocalIl2CppData(string editorIl2cppPath, string libil2cppWithHybridclrSourceDir, UnityVersion version)
        {
            if (!IsCompatibleVersion())
            {
                Debug.LogError($"il2cpp 版本不兼容，最小版本为 {GetCurrentUnityVersionMinCompatibleVersionStr()}");
                return;
            }
            string workDir = SettingsUtil.HybridCLRDataDir;
            Directory.CreateDirectory(workDir);

            // create LocalIl2Cpp
            string localUnityDataDir = SettingsUtil.LocalUnityDataDir;
            BashUtil.RecreateDir(localUnityDataDir);

            // copy MonoBleedingEdge
            BashUtil.CopyDir($"{Directory.GetParent(editorIl2cppPath)}/MonoBleedingEdge", $"{localUnityDataDir}/MonoBleedingEdge", true);

            // copy il2cpp
            BashUtil.CopyDir(editorIl2cppPath, SettingsUtil.LocalIl2CppDir, true);

            // replace libil2cpp
            string dstLibil2cppDir = $"{SettingsUtil.LocalIl2CppDir}/libil2cpp";
            BashUtil.CopyDir($"{libil2cppWithHybridclrSourceDir}", dstLibil2cppDir, true);

            // clean Il2cppBuildCache
            BashUtil.RemoveDir($"{SettingsUtil.ProjectDir}/Library/Il2cppBuildCache", true);

            if (HasInstalledHybridCLR())
            {
                Debug.Log("安装成功");
            }
            else
            {
                Debug.LogError("安装失败");
            }
        }
    }
}
