using System;
using System.IO;
using UnityEditor;
using Debug = UnityEngine.Debug;
using System.Text.RegularExpressions;
using UnityEngine;

namespace HybridCLR.Editor.Installer
{
    public enum InstallErrorCode
    {
        Ok,
    }

    public class InstallerController
    {
        private EditorWindow window;

        private const string hybridclr_repo_path = "hybridclr_repo";
        private const string il2cpp_plus_repo_path = "il2cpp_plus_repo";
        public int MajorVersion => _curVersion.major;

        private UnityVersion _curVersion;
        public InstallerController(EditorWindow window)
        {
            this.window = window;
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
            switch (majorVersion)
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

        public void InitHybridCLR(string il2cppBranch, VersionResolver hybridCLRVer, VersionResolver il2cppPlusVer)
        {
            var il2cppInstallPath = GetIl2CppPathByContentPath(EditorApplication.applicationContentsPath);
            RunInitLocalIl2CppData(il2cppBranch, il2cppInstallPath, hybridCLRVer, il2cppPlusVer);
        }

        private string GetIl2CppPathByContentPath(string contentPath) => $"{contentPath}/il2cpp";
        public static bool HasInstalledHybridCLR => Directory.Exists($"{SettingsUtil.LocalIl2CppDir}/libil2cpp/hybridclr");

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

        private void RunInitLocalIl2CppData(string il2cppBranch, string editorIl2cppPath, VersionResolver resolver_h, VersionResolver resolver_i)
        {
#if UNITY_EDITOR_WIN
            if (!BashUtil.ExistProgram("git"))
            {
                throw new Exception($"安装本地il2cpp需要使用git从远程拉取仓库，请先安装git");
            }
#endif
            //Draw a editor progress bar
            EditorUtility.DisplayProgressBar("HybridCLR", "正在检出仓库数据...", 0.1f);
            try
            {
                // checkout hybridclr
                resolver_h.InitRepository(); //防止用户删除安装器构建的文件系统,下同
                var result = resolver_h.FetchRepoTags();
                if (result != 0)
                {
                    throw new Exception($"检出hybridclr仓库失败，错误码：{result}");
                }
                else
                {
                    resolver_h.CheckoutSelectedTag();
                }
                // checkout il2cpp_plus
                resolver_i.InitRepository();
                result = resolver_i.FetchRepoTags();
                if (result != 0)
                {
                    throw new Exception($"检出 il2cppplus 仓库失败，错误码：{result}");
                }
                else
                {
                    resolver_i.CheckoutSelectedTag();
                }
            }
            catch (Exception e)
            {
                window.ShowNotification(new UnityEngine.GUIContent("HybridCLR 安装失败"));
                EditorUtility.ClearProgressBar();
                throw e;
            }

            EditorUtility.DisplayProgressBar("HybridCLR", "正在对拷 IOSBuild 数据...", 0.2f);
            string workDir = SettingsUtil.HybridCLRDataDir;
            string buildiOSDir = $"{workDir}/iOSBuild";
            BashUtil.RemoveDir(buildiOSDir);
            BashUtil.CopyDir($"{SettingsUtil.HybridCLRDataPathInPackage}/iOSBuild", buildiOSDir, true);

            // create LocalIl2Cpp
            string localUnityDataDir = SettingsUtil.LocalUnityDataDir;
            BashUtil.RecreateDir(localUnityDataDir);

            // copy MonoBleedingEdge
            EditorUtility.DisplayProgressBar("HybridCLR", "正在对拷 MonoBleedingEdge 数据...", 0.5f);
            BashUtil.CopyDir($"{Directory.GetParent(editorIl2cppPath)}/MonoBleedingEdge", $"{localUnityDataDir}/MonoBleedingEdge", true);

            // copy il2cpp
            EditorUtility.DisplayProgressBar("HybridCLR", "正在对拷 il2cpp 数据...", 0.7f);
            BashUtil.CopyDir(editorIl2cppPath, SettingsUtil.LocalIl2CppDir, true);

            // replace libil2cpp
            EditorUtility.DisplayProgressBar("HybridCLR", "正在对拷仓库数据...", 0.90f);
            DuplicateRepoData();
            UnityVersion version = _curVersion;

            if (version.major == 2019)
            {
                string curVersionStr = version.ToString();
                string srcIl2CppDll = GetUnityIl2CppDllModifiedPath(curVersionStr);
                if (File.Exists(srcIl2CppDll))
                {
                    string dstIl2CppDll = GetUnityIl2CppDllInstallLocation();
                    EditorUtility.DisplayProgressBar("HybridCLR", "正在对拷 Il2CppDllModified 数据...", 1.0f);
                    File.Copy(srcIl2CppDll, dstIl2CppDll, true);
                    Debug.Log($"copy {srcIl2CppDll} => {dstIl2CppDll}");
                }
                else
                {
                    Debug.LogError($"未找到当前版本:{curVersionStr} 对应的改造过的 Unity.IL2CPP.dll，打包出的程序将会崩溃");
                }
            }
            EditorUtility.ClearProgressBar();
            var msg = $"HybridCLR 安装{(HasInstalledHybridCLR ? "成功" : "失败")}";
            window.ShowNotification(new UnityEngine.GUIContent(msg));
            Debug.Log($"{nameof(InstallerController)}: {msg}");
        }

        /// <summary>
        /// 清空由之前仓库提供的核心 hybridclr 和 il2cpp_plus 数据
        /// </summary>
        public static void DuplicateRepoData()
        {
            string dstLibil2cppDir = $"{SettingsUtil.LocalIl2CppDir}/libil2cpp";
            string hybridclrRepoDir = $"{SettingsUtil.GetRepositoryLocation(Repository.HybridCLR)}/hybridclr";
            string il2cppPlusRepoDir = $"{SettingsUtil.GetRepositoryLocation(Repository.IL2CPP_Plus)}/libil2cpp";
            // 单方面调用也会触发俩仓库全员对拷，不是最好，但是也没什么问题。
            // 毕竟 libil2cpp 参与是否成功安装的判断,当前举措是简化了编码，机器多了些重复拷贝行为
            //  另一方面，仓库本身体量极小，用户安装体验影响不大
            BashUtil.RemoveDir(dstLibil2cppDir);
            if (Directory.Exists(il2cppPlusRepoDir) && Directory.Exists(hybridclrRepoDir))
            {
                BashUtil.CopyDir(il2cppPlusRepoDir, dstLibil2cppDir);
                BashUtil.CopyDir(hybridclrRepoDir, $"{dstLibil2cppDir}/hybridclr");
            }
            // clean Il2cppBuildCache
            BashUtil.RemoveDir($"{SettingsUtil.ProjectDir}/Library/Il2cppBuildCache", true);
        }
    }
}
