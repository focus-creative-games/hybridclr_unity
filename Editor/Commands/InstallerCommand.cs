using HybridCLR.Editor.Installer;
using UnityEditor;
using UnityEngine;

namespace HybridCLR.Editor.Commands
{
    public static class InstallerCommand
    {

        [MenuItem("HybridCLR/Installer...", false, 0)]
        private static void Open()
        {
            InstallerWindow window = EditorWindow.GetWindow<InstallerWindow>("HybridCLR Installer", true);
            window.minSize = new Vector2(500f, 300f);
        }
    }
}
