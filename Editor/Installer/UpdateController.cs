namespace HybridCLR.Editor.Installer
{
    public partial class InstallerController
    {
        public bool HasUpdateIl2Cpp(string il2cppBranch)
        {
            string workDir = SettingsUtil.HybridCLRDataDir;
            // last hash hybridclr
            {
                string hybridclrRepoDir = $"{workDir}/{hybridclr_repo_path}";
                var ret1 = BashUtil.RunCommand2(hybridclrRepoDir, "git",
                    new string[] { "log", "HEAD", "-n", "1", "--pretty=format:\"%H\"", });
                BashUtil.RunCommand2(hybridclrRepoDir, "git",
                    new string[] { "fetch", "depth=1" });
                var ret2 = BashUtil.RunCommand2(hybridclrRepoDir, "git",
                    new string[] { "log", "remotes/origin/HEAD", "-n", "1", "--pretty=format:\"%H\"", });
                if (ret1.StdOut != ret2.StdOut)
                {
                    return true;
                }
            }
            // last hash il2cpp_plus
            {
                string il2cppPlusRepoDir = $"{workDir}/{il2cpp_plus_repo_path}";
                var ret1 = BashUtil.RunCommand2(il2cppPlusRepoDir, "git",
                    new string[] { "log", $"{il2cppBranch}", "-n", "1", "--pretty=format:\"%H\"", });
                BashUtil.RunCommand2(il2cppPlusRepoDir, "git",
                    new string[] { "fetch", "depth=1" });
                var ret2 = BashUtil.RunCommand2(il2cppPlusRepoDir, "git",
                    new string[] { "log", $"remotes/origin/{il2cppBranch}", "-n", "1", "--pretty=format:\"%H\"", });
                if (ret1.StdOut != ret2.StdOut)
                {
                    return true;
                }
            }
            return false;
        }
    }
}