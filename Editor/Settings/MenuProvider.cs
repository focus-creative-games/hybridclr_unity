using UnityEditor;
using UnityEngine;

public static class MenuProvider
{
    [MenuItem("HybridCLR/Settings", priority = 200)]
    public static void OpenSettings() => SettingsService.OpenProjectSettings("Project/HybridCLR Settings");
    [MenuItem("HybridCLR/About us", priority = 400)]
    public static void AboutUs() => Application.OpenURL("https://focus-creative-games.github.io/hybridclr/about/");

    [MenuItem("HybridCLR/Documents", priority = 399)]
    public static void OpenDoc() => Application.OpenURL("https://focus-creative-games.github.io/hybridclr/");
}
