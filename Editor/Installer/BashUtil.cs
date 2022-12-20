using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;

namespace HybridCLR.Editor.Installer
{
    public static class BashUtil
    {
        public static int RunCommand(string workingDir, string program, string[] args, bool log = true)
        {
            using (Process p = new Process())
            {
                p.StartInfo.WorkingDirectory = workingDir;
                p.StartInfo.FileName = program;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                string argsStr = string.Join(" ", args.Select(arg => "\"" + arg + "\""));
                p.StartInfo.Arguments = argsStr;
                if (log)
                {
                    UnityEngine.Debug.Log($"[BashUtil] run => {program} {argsStr}");
                }
                p.Start();
                p.WaitForExit();
                return p.ExitCode;
            }
        }


        public static (int ExitCode, string StdOut, string StdErr) RunCommand2(string workingDir, string program, string[] args, bool log = true)
        {
            using (Process p = new Process())
            {
                p.StartInfo.WorkingDirectory = workingDir;
                p.StartInfo.FileName = program;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                string argsStr = string.Join(" ", args.Select(arg => "\"" + arg + "\""));
                p.StartInfo.Arguments = argsStr;
                if (log)
                {
                    UnityEngine.Debug.Log($"[BashUtil] run => {program} {argsStr}");
                }
                p.Start();
                p.WaitForExit();

                string stdOut = p.StandardOutput.ReadToEnd();
                string stdErr = p.StandardError.ReadToEnd();
                return (p.ExitCode, stdOut, stdErr);
            }
        }

        public static bool ExistProgram(string prog)
        {
#if UNITY_EDITOR_WIN
            return RunCommand(".", "where", new string[] { prog }) == 0;
#elif UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
            return RunCommand(".", "which", new string[] {prog}) == 0;
#endif
        }


        public static void RemoveDir(string dir, bool log = false)
        {
            if (log)
            {
                UnityEngine.Debug.Log($"[BashUtil] RemoveDir dir:{dir}");
            }
            if (!Directory.Exists(dir))
            {
                return;
            }
            foreach (var file in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }
            Directory.Delete(dir, true);
        }

        public static void RecreateDir(string dir)
        {
            if (Directory.Exists(dir))
            {
                RemoveDir(dir, true);
            }
            Directory.CreateDirectory(dir);
        }

        public static void CopyDir(string src, string dst, bool log = false)
        {
            if (log)
            {
                UnityEngine.Debug.Log($"[BashUtil] CopyDir {src} => {dst}");
            }
            RecreateDir(dst);
            foreach (string dirPath in Directory.GetDirectories(src, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(src, dst));
            }
            foreach (string newPath in Directory.GetFiles(src, "*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(src, dst), true);
            }
        }
    }
}
