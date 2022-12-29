using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Diagnostics;

using Debug = UnityEngine.Debug;
using System.Text.RegularExpressions;

namespace HybridCLR.Editor.Installer
{
    public enum InstallErrorCode
    {
        Ok,
    }

    public partial class InstallerController
    {
        private const string hybridclr_repo_path = "hybridclr_repo";

        private const string il2cpp_plus_repo_path = "il2cpp_plus_repo";
        


        public int MajorVersion => _curVersion.major;

        private UnityVersion _curVersion;

        public InstallerController()
        {
            _curVersion = ParseUnityVersion(Application.unityVersion);
        }

        public class UnityVersion
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

        public const int min2019_4_CompatibleMinorVersion = 40;
        public const int min2020_3_CompatibleMinorVersion = 21;
        public const int min2021_3_CompatibleMinorVersion = 0;

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
                case 2019: return $"2019.4.{min2019_4_CompatibleMinorVersion}";
                case 2020: return $"2020.3.{min2020_3_CompatibleMinorVersion}";
                case 2021: return $"2021.3.{min2021_3_CompatibleMinorVersion}";
                default: throw new Exception($"not support version:{majorVersion}");
            }
        }

        public bool IsComaptibleVersion()
        {
            UnityVersion version = _curVersion;
            switch (version.major)
            {
                case 2019:
                    {
                        if (version.major != 2019 || version.minor1 != 4)
                        {
                            return false;
                        }
                        return version.minor2 >= min2019_4_CompatibleMinorVersion;
                    }
                case 2020:
                    {
                        if (version.major != 2020 || version.minor1 != 3)
                        {
                            return false;
                        }
                        return version.minor2 >= min2020_3_CompatibleMinorVersion;
                    }
                case 2021:
                    { 
                        if (version.major != 2021 || version.minor1 != 3)
                        {
                            return false;
                        }
                        return version.minor2 >= min2021_3_CompatibleMinorVersion;
                    }
                default: throw new Exception($"not support il2cpp_plus branch:{version.major}");
            }
        }

        private string _hybridclrLocalVersion;

        public string HybridclrLocalVersion => _hybridclrLocalVersion != null ? _hybridclrLocalVersion : _hybridclrLocalVersion = GetHybridCLRLocalVersion();


        public string HybridCLRRepoInstalledVersion
        {
            get { return EditorPrefs.GetString($"hybridclr_repo#{MajorVersion}"); }
            set { EditorPrefs.SetString($"hybridclr_repo#{MajorVersion}", value); }
        }

        public string Il2CppRepoInstalledVersion
        {
            get { return EditorPrefs.GetString($"il2cpp_plus_repo#{MajorVersion}"); }
            set { EditorPrefs.SetString($"il2cpp_plus_repo#{MajorVersion}", value); }
        }


        private string GetHybridCLRLocalVersion()
        {
            string workDir = SettingsUtil.HybridCLRDataDir;
            string hybridclrRepoDir = $"{workDir}/{hybridclr_repo_path}";
            if (Directory.Exists(hybridclrRepoDir))
            {
                var ret = BashUtil.RunCommand2(hybridclrRepoDir, "git",
                    new string[] { "log", "HEAD", "-n", "1", "--pretty=format:\"%H\"", },
                    false);
                if (ret.ExitCode == 0)
                {
                    return ret.StdOut.Trim();
                }
                else
                {
                    return "ERROR";
                }
            }
            return "";
        }

        private string _il2cppPlusLocalVersion;

        public string Il2cppPlusLocalVersion => _il2cppPlusLocalVersion != null ? _il2cppPlusLocalVersion : _il2cppPlusLocalVersion = GetIl2cppPlusLocalVersion();

        private string GetIl2cppPlusLocalVersion()
        {
            string workDir = SettingsUtil.HybridCLRDataDir;
            string il2cppPlusRepoDir = $"{workDir}/{il2cpp_plus_repo_path}";
            if (Directory.Exists(il2cppPlusRepoDir))
            {
                var ret = BashUtil.RunCommand2(il2cppPlusRepoDir, "git",
                    new string[] { "log", "HEAD", "-n", "1", "--pretty=format:\"%H\"", },
                    false);
                if (ret.ExitCode == 0)
                {
                    return ret.StdOut.Trim();
                }
                else
                {
                    return "ERROR";
                }
            }
            return "";
        }

        private string GetIl2CppPathByContentPath(string contentPath)
        {
            return $"{contentPath}/il2cpp";
        }

        public void InstallLocalHybridCLR(string hybridclrVer, string il2cppPlusVer)
        {
            RunInitLocalIl2CppData(GetIl2CppPathByContentPath(EditorApplication.applicationContentsPath), _curVersion, hybridclrVer, il2cppPlusVer);
        }

        public bool HasInstalledHybridCLR()
        {
            return Directory.Exists($"{SettingsUtil.LocalIl2CppDir}/libil2cpp/hybridclr");
        }


        private string GetUnityIl2CppDllInstallLocation()
        {
#if UNITY_EDITOR_WIN
            return $"{SettingsUtil.LocalIl2CppDir}/build/deploy/net471/Unity.IL2CPP.dll";
#else
            return $"{SettingsUtil.LocalIl2CppDir}/build/deploy/il2cppcore/Unity.IL2CPP.dll";
#endif
        }

        private string GetUnityIl2CppDllModifiedPath(string curVersionStr)
        {
#if UNITY_EDITOR_WIN
            return $"{SettingsUtil.ProjectDir}/{SettingsUtil.HybridCLRDataPathInPackage}/ModifiedUnityAssemblies/{curVersionStr}/Unity.IL2CPP-Win.dll.bytes";
#else
            return $"{SettingsUtil.ProjectDir}/{SettingsUtil.HybridCLRDataPathInPackage}/ModifiedUnityAssemblies/{curVersionStr}/Unity.IL2CPP-Mac.dll.bytes";
#endif
        }

        private void RunInitLocalIl2CppData(string editorIl2cppPath, UnityVersion version, string hybridclrVer, string il2cppPlusVer)
        {
#if UNITY_EDITOR_WIN
            if (!BashUtil.ExistProgram("git"))
            {
                throw new Exception($"安装本地il2cpp需要使用git从远程拉取仓库，请先安装git");
            }
#endif

            string workDir = SettingsUtil.HybridCLRDataDir;
            Directory.CreateDirectory(workDir);
            //BashUtil.RecreateDir(workDir);

            string buildiOSDir = $"{workDir}/iOSBuild";
            BashUtil.RemoveDir(buildiOSDir);
            BashUtil.CopyDir($"{SettingsUtil.HybridCLRDataPathInPackage}/iOSBuild", buildiOSDir, true);

            // clone hybridclr
            string hybridclrRepoURL = HybridCLRSettings.Instance.hybridclrRepoURL;
            string hybridclrRepoDir = $"{workDir}/{hybridclr_repo_path}";
            {
                BashUtil.RemoveDir(hybridclrRepoDir);
                string[] args = new string[]
                {
                "clone",
                "--depth=1",
                "-b",
                hybridclrVer,
                hybridclrRepoURL,
                hybridclrRepoDir,
                };
                var ret = BashUtil.RunCommand(workDir, "git", args);
                //if (ret != 0)
                //{
                //    throw new Exception($"git clone 失败");
                //}
            }

            // clone il2cpp_plus
            string il2cppPlusRepoURL = HybridCLRSettings.Instance.il2cppPlusRepoURL;
            string il2cppPlusRepoDir = $"{workDir}/{il2cpp_plus_repo_path}";
            {
                BashUtil.RemoveDir(il2cppPlusRepoDir);
                string[] args = new string[]
                {
                "clone",
                "--depth=1",
                "-b",
                il2cppPlusVer,
                il2cppPlusRepoURL,
                il2cppPlusRepoDir,
                };
                var ret = BashUtil.RunCommand(workDir, "git", args);
                //if (ret != 0)
                //{
                //    throw new Exception($"git clone 失败");
                //}
            }

            // create LocalIl2Cpp
            string localUnityDataDir = SettingsUtil.LocalUnityDataDir;
            BashUtil.RecreateDir(localUnityDataDir);

            // copy MonoBleedingEdge
            BashUtil.CopyDir($"{Directory.GetParent(editorIl2cppPath)}/MonoBleedingEdge", $"{localUnityDataDir}/MonoBleedingEdge", true);

            // copy il2cpp
            BashUtil.CopyDir(editorIl2cppPath, SettingsUtil.LocalIl2CppDir, true);

            // replace libil2cpp
            string dstLibil2cppDir = $"{SettingsUtil.LocalIl2CppDir}/libil2cpp";
            BashUtil.CopyDir($"{il2cppPlusRepoDir}/libil2cpp", dstLibil2cppDir, true);
            BashUtil.CopyDir($"{hybridclrRepoDir}/hybridclr", $"{dstLibil2cppDir}/hybridclr", true);

            // clean Il2cppBuildCache
            BashUtil.RemoveDir($"{SettingsUtil.ProjectDir}/Library/Il2cppBuildCache", true);

            if (version.major == 2019)
            {
                string curVersionStr = version.ToString();
                string srcIl2CppDll = GetUnityIl2CppDllModifiedPath(curVersionStr);
                if (File.Exists(srcIl2CppDll))
                {
                    string dstIl2CppDll = GetUnityIl2CppDllInstallLocation();
                    File.Copy(srcIl2CppDll, dstIl2CppDll, true);
                    Debug.Log($"copy {srcIl2CppDll} => {dstIl2CppDll}");
                }
                else
                {
                    Debug.LogError($"未找到当前版本:{curVersionStr} 对应的改造过的 Unity.IL2CPP.dll，打包出的程序将会崩溃");
                }
            }
            if (HasInstalledHybridCLR())
            {
                Debug.Log("安装成功！");
                _hybridclrLocalVersion = null;
                _il2cppPlusLocalVersion = null;
                HybridCLRRepoInstalledVersion = hybridclrVer;
                Il2CppRepoInstalledVersion = il2cppPlusVer;
            }
            else
            {
                Debug.LogError("安装失败！");
            }
        }
    }
}
