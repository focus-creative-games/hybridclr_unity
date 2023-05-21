using HybridCLR.Editor.Installer;
using UnityEditor;
using UnityEngine;

public static class MenuProvider
{

    [MenuItem("HybridCLR/About HybridCLR", priority = 0)]
    public static void OpenAbout() => Application.OpenURL("https://hybridclr.doc.code-philosophy.com/#/README");

    [MenuItem("HybridCLR/Installer...", priority = 60)]
    private static void Open()
    {
        InstallerWindow window = EditorWindow.GetWindow<InstallerWindow>("HybridCLR Installer", true);
        window.minSize = new Vector2(800f, 500f);
    }

    [MenuItem("HybridCLR/Settings...", priority = 61)]
    public static void OpenSettings() => SettingsService.OpenProjectSettings("Project/HybridCLR Settings");

    [MenuItem("HybridCLR/Documents/Quick Start")]
    public static void OpenQuickStart() => Application.OpenURL("https://hybridclr.doc.code-philosophy.com/#/beginner/quickstart");

    [MenuItem("HybridCLR/Documents/Benchmark")]
    public static void OpenBenchmark() => Application.OpenURL("https://hybridclr.doc.code-philosophy.com/#/basic/performance");

    [MenuItem("HybridCLR/Documents/FAQ")]
    public static void OpenFAQ() => Application.OpenURL("https://hybridclr.doc.code-philosophy.com/#/help/faq");

    [MenuItem("HybridCLR/Documents/Common Errors")]
    public static void OpenCommonErrors() => Application.OpenURL("https://hybridclr.doc.code-philosophy.com/#/help/commonerrors");

    [MenuItem("HybridCLR/Documents/Bug Report")]
    public static void OpenBugReport() => Application.OpenURL("https://hybridclr.doc.code-philosophy.com/#/help/issue");
}

