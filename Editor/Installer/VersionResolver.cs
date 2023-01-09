using HybridCLR.Editor.Installer;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
namespace HybridCLR.Editor
{
    public class VersionResolver
    {
        public string[] tags;
        public string selected; // selected 作用于 popup 菜单
        public string current; // current 仅记录当前 checkout 的分支
        public int index;
        public bool isInItializing; //仓库是否正在初始化
        public bool isChecking;//是否正在 checkout 分支
        public string branch;
        public string Path => GetRepositoryLocation();
        public bool IsValid => !isInItializing && !isChecking;

        public VersionResolver(Repository repository, EditorWindow window, bool log = false, string branch = "")
        {
            this.log = log;
            this.repository = repository;
            this.branch = branch;
            this.window = window;
            _=FetchRepositoryDataAsync();
        }

        #region 仓库初始化
        public void InitRepository()
        {
            if (!IsGitRepository)
            {
                string repo_url = GetRepositoryUrl();
                var dirInfo = new DirectoryInfo(Path);
                if (!dirInfo.Exists)
                {
                    dirInfo.Create();
                }
                //init git
                BashUtil.RunCommand(Path, "git", new string[] { "init" }, false);
                // add remote origin
                BashUtil.RunCommand(Path, "git", new string[] { "remote", "add", "origin", repo_url }, false);
                // add repo path as a safe directory
                //https://stackoverflow.com/questions/71849415/i-cannot-add-the-parent-directory-to-safe-directory-in-git
                //path = path.Replace("\\", "/");
                //BashUtil.RunCommand(path, "git", new string[] { "config", "--global", "--add", "safe.directory", path }, false);
            }
        }

        /// <summary>
        ///  从远程仓库快速获取全部的 tag 值
        /// </summary>
        public int FetchRepoTags()
        {
            //Fast fetch repo to get refs
            //https://github.com/mob-sakai/UpmGitExtension/blob/main/Editor/Commands/fetch-packages.js
            //git fetch --depth=1 -fq --prune origin "refs/tags/*:refs/tags/*" "+refs/heads/*:refs/remotes/origin/*"
            var value = BashUtil.RunCommand2(Path, "git", new string[] { "fetch", "--depth=1", "-fq", "--prune", "origin", "refs/tags/*:refs/tags/*", "+refs/heads/*:refs/remotes/origin/*" }, false);
            if (value.ExitCode != 0)
            {
                Debug.LogError($"{nameof(VersionResolver)} - {repository} - {value.StdErr}");
            }
            return value.ExitCode;
        }
        #endregion

        public async Task FetchRepositoryDataAsync(bool forceFetchTags = false)
        {
            isInItializing = true;
            ReloadRepositoryIfNeeded();
            if (!IsGitRepository || forceFetchTags) // 当且仅当仓库不存在时，才会执行初始化，并获取 tags 数据，避免每次开启安装器都拉取
            {
                InitRepository();
                await Task.Run(FetchRepoTags);
            }
            current = selected = GetRepositoryLocalVersion();
            try
            {
                tags = GetRepositoryRemoteVersion();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            if (!string.IsNullOrEmpty(branch))//过滤前缀，前缀约定为 Unity 版本
            {
                tags = tags.Where(v => v.StartsWith($"v{branch}")).ToArray();
            }
            index = Array.IndexOf(tags, selected);
            // 当仓库未checkout任何tag时，赋初始值为本地最新版tag，如果没有tag，则赋值为 NONEINFO
            //  每次 checkout tag 时，都会更新 selected 为第一个。
            if (index != 0)
            {
                index = 0;
                if (tags.Length == 0)
                {
                    var defaultOne = current != "" && current != ERRORGITINFO ? current : NONEINFO;
                    tags = new string[] { defaultOne };
                }
                selected = tags[0];
            }
            isInItializing = false;
            Loom.Post(() => window?.Repaint());
            if (log)
            {
                Debug.Log($"{nameof(VersionResolver)}: current = {current}, selected = {selected}");
                Debug.Log($"{repository} {nameof(tags)}: {string.Join(" , ", tags)}");
                Debug.Log($"{repository} {nameof(selected)}: {selected}");
                Debug.Log($"{repository} index = {index}");
            }
        }

        internal void DrawVersionSelector()
        {
            var RECT = GUILayoutUtility.GetLastRect();
            RECT.x += 120;
            RECT.width = 150;
            if (!isInItializing)
            {
                var color = GUI.color;
                GUI.color = selected == NONEINFO ? Color.red : color;
                index = EditorGUI.Popup(RECT, index, tags);
                GUI.color = color;
                if (selected != tags[index])
                {
                    selected = tags[index];
                }
                if (selected != current && selected != NONEINFO && current != ERRORGITINFO && InstallerController.HasInstalledHybridCLR)
                {
                    RECT.x += 155;
                    RECT.width = 68;
                    if (GUI.Button(RECT, content_btn_ck))
                    {
                        CheckoutSelectedTag();
                        InstallerController.DuplicateRepoData();
                    }
                }
            }
            else
            {
                style = style ?? new GUIStyle(EditorStyles.boldLabel);
                style.normal.textColor = Color.green;
                EditorGUI.LabelField(RECT, "Checking...", style);
            }
        }

        public bool HasUpdate => !isInItializing && !isChecking && tags != null && tags[0] != current && tags[0] != NONEINFO && (current != ERRORGITINFO || current != NONEINFO);
        public async Task<(bool hasUpdate, string tag)> CheckUpdateAsync()
        {
            await FetchRepositoryDataAsync(true); //检查更新时，强制拉取 tags
            string lastTag = tags[0];
            if (HasUpdate)
            {
                return (true, lastTag);
            }
            return (false, current);
        }

        public string GetRepositoryLocalVersion()
        {
            //git tag --points-at HEAD
            //https://stackoverflow.com/questions/44627880/how-to-tell-if-current-revision-is-tagged
            var ret = BashUtil.RunCommand2(Path, "git", new string[] { "tag", "--points-at", "HEAD" }, false);
            if (ret.ExitCode == 0)
            {
                var tags = ret.StdOut.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (tags.Length > 1)
                {
                    Debug.LogError($"{nameof(VersionResolver)}: {repository} has multiple tags: {string.Join(" , ", tags)}");
                }
                if (tags.Length == 0)
                {
                    Debug.LogWarning($"{nameof(VersionResolver)}: {repository} HEAD has no tag");
                    return NONEINFO;
                }
                return tags[0];
            }
            return ERRORGITINFO;
        }

        public string[] GetRepositoryRemoteVersion()
        {
            var ret = BashUtil.RunCommand2(Path, "git", new string[] { "tag" }, false);
            var tags = ret.StdOut.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            // 使用 semver 的版本排序
            Array.Sort(tags, (a, b) => SemverCompare(a, b));
            return tags;
        }

        /// <summary>
        /// 获取仓库中记录的 origin remote url 信息
        /// </summary>
        private string GetRepositoryOriginUrl()
        {
            var result = BashUtil.RunCommand2(Path, "git", new string[] { "remote", "get-url", "origin" }, false);
            return result.StdOut.Trim();
        }

        #region 检出指定的 Tag 
        public void CheckoutLatestTag() => CheckoutSpecificTag(tags[0]);
        public void CheckoutSelectedTag() => CheckoutSpecificTag(selected);
        public void CheckoutSpecificTag(string tag)
        {
            if (tag == NONEINFO)
            {
                return;
            }
            //git checkout tags/<tag>
            isChecking = true;
            var ret = BashUtil.RunCommand2(Path, "git", new string[] { "checkout", $"tags/{tag}" }, false);
            if (ret.ExitCode == 0)
            {
                current = selected = tag;
                index = Array.IndexOf(tags, selected);
            }
            else
            {
                Debug.LogError($"{nameof(VersionResolver)}: {repository} checkout faild, exitcode = {ret.ExitCode}, error = {ret.StdErr}");
            }
            isChecking = false;
            Loom.Post(() => window?.Repaint());
        }
        #endregion

        #region Assistant Function
        // v2021-1.10.1-rc-dhe
        // get semver version from tag
        public Version ResolveVersionFromTag(string tag)
        {
            var pattern = string.IsNullOrEmpty(branch) ? @"v(\d+)\.(\d+)\.(\d+)" : @"-(\d+)\.(\d+)\.(\d+)";
            var match = Regex.Match(tag, pattern);
            if (match.Success)
            {
                var major = int.Parse(match.Groups[1].Value);
                var minor = int.Parse(match.Groups[2].Value);
                var patch = int.Parse(match.Groups[3].Value);
                return new Version(major, minor, patch);
            }
            else
            {
                throw new Exception($"tag = {tag} 格式不正确，约定 tag 格式为：v1.0.0-rc 或者 v2020-1.0.0-rc ");
            }
        }

        private int SemverCompare(string a, string b)
        {
            Version va = ResolveVersionFromTag(a);
            Version vb = ResolveVersionFromTag(b);
            return vb.CompareTo(va);
        }

        private string GetRepositoryUrl()
        {
            switch (repository)
            {
                case Repository.HybridCLR:
                    return HybridCLRSettings.Instance.hybridclrRepoURL;
                case Repository.IL2CPP_Plus:
                    return HybridCLRSettings.Instance.il2cppPlusRepoURL;
            }
            return null;
        }
        private string GetRepositoryLocation()
        {
            switch (repository)
            {
                case Repository.HybridCLR:
                    return $"{SettingsUtil.HybridCLRDataDir}/{SettingsUtil.HybridCLRRepoFolder}";
                case Repository.IL2CPP_Plus:
                    return $"{SettingsUtil.HybridCLRDataDir}/{SettingsUtil.Il2cppPlusRepoFolder}";
            }
            return null;
        }

        private bool IsGitRepository => Directory.Exists(System.IO.Path.Combine(Path, ".git"));

        public bool ReloadRepositoryIfNeeded()
        {
            string repoUrl = GetRepositoryUrl();

            if (IsGitRepository)
            {
                string remoteUrl = GetRepositoryOriginUrl();
                if (!string.IsNullOrEmpty(remoteUrl) && remoteUrl != repoUrl)
                {
                    BashUtil.RemoveDir(Path);
                    if (InstallerController.HasInstalledHybridCLR)
                    {
                        Debug.LogWarning($"{repository} 仓库链接被改变，请点击“检查更新“应用变更。more ↓ \nold = {remoteUrl}, \nnew = {repoUrl}");
                    }
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region Private Fields
        readonly GUIContent content_btn_ck = new GUIContent("Checkout", "点击即可检出你所选择的版本,在已经安装的情况下无需全新安装！");
        GUIStyle style;
        private readonly bool log;
        readonly Repository repository;
        readonly EditorWindow window;
        const string NONEINFO = "- No Tag Found -";
        const string ERRORGITINFO = "ERROR Or Not Installed";
        #endregion
    }
}
