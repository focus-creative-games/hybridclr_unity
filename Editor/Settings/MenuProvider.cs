using HybridCLR.Editor.Installer;
using HybridCLR.Editor.Tool;
using UnityEditor;
using UnityEngine;

namespace HybridCLR.Editor.Settings
{
    public static class MenuProvider
    {

        [MenuItem("HybridCLR/About HybridCLR", priority = 0)]
        public static void OpenAbout() => Application.OpenURL("https://hybridclr.doc.code-philosophy.com/docs/intro");

        [MenuItem("HybridCLR/Installer...", priority = 60)]
        private static void Open()
        {
            InstallerWindow window = EditorWindow.GetWindow<InstallerWindow>("HybridCLR Installer", true);
            window.minSize = new Vector2(800f, 500f);
        }

        [MenuItem("HybridCLR/Settings...", priority = 61)]
        public static void OpenSettings() => SettingsService.OpenProjectSettings("Project/HybridCLR Settings");

        [MenuItem("HybridCLR/Documents/Quick Start")]
        public static void OpenQuickStart() => Application.OpenURL("https://hybridclr.doc.code-philosophy.com/docs/beginner/quickstart");

        [MenuItem("HybridCLR/Documents/Performance")]
        public static void OpenPerformance() => Application.OpenURL("https://hybridclr.doc.code-philosophy.com/docs/basic/performance");

        [MenuItem("HybridCLR/Documents/FAQ")]
        public static void OpenFAQ() => Application.OpenURL("https://hybridclr.doc.code-philosophy.com/docs/help/faq");

        [MenuItem("HybridCLR/Documents/Common Errors")]
        public static void OpenCommonErrors() => Application.OpenURL("https://hybridclr.doc.code-philosophy.com/docs/help/commonerrors");

        [MenuItem("HybridCLR/Documents/Bug Report")]
        public static void OpenBugReport() => Application.OpenURL("https://hybridclr.doc.code-philosophy.com/docs/help/issue");

        [MenuItem("HybridCLR/Tool/CopyDllsWithExtension/AOTDlls")]
        public static void CopyAOTDlls() => CopyDlls.CopyAOTDlls();

        [MenuItem("HybridCLR/Tool/CopyDllsWithExtension/HotUpdateDlls")]
        public static void CopyHotUpdateDlls() => CopyDlls.CopyHotUpdateDlls();
    }
}