// Copyright 2026 Code Philosophy
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using HybridCLR.Installer;
using UnityEditor;
using UnityEngine;

namespace HybridCLR
{
    public static class MenuProvider
    {
        public const string WebsiteUrl = "https://www.hybridclr.cn";

        [MenuItem("HybridCLR/About", priority = 0)]
        public static void OpenAbout() => Application.OpenURL($"{WebsiteUrl}/docs/intro");

        [MenuItem("HybridCLR/Installer...", priority = 60)]
        private static void Open()
        {
            InstallerWindow window = EditorWindow.GetWindow<InstallerWindow>("HybridCLR Installer", true);
            window.minSize = new Vector2(800f, 500f);
        }

        [MenuItem("HybridCLR/Settings...", priority = 61)]
        public static void OpenSettings() => SettingsService.OpenProjectSettings("Project/HybridCLR Settings");

        [MenuItem("HybridCLR/Documents/Quick Start")]
        public static void OpenQuickStart() => Application.OpenURL($"{WebsiteUrl}/docs/beginner/quickstart");

        [MenuItem("HybridCLR/Documents/Performance")]
        public static void OpenPerformance() => Application.OpenURL($"{WebsiteUrl}/docs/basic/performance");

        [MenuItem("HybridCLR/Documents/FAQ")]
        public static void OpenFAQ() => Application.OpenURL($"{WebsiteUrl}/docs/help/faq");

        [MenuItem("HybridCLR/Documents/Common Errors")]
        public static void OpenCommonErrors() => Application.OpenURL($"{WebsiteUrl}/docs/help/commonerrors");

        [MenuItem("HybridCLR/Documents/Bug Report")]
        public static void OpenBugReport() => Application.OpenURL($"{WebsiteUrl}/docs/help/issue");
    }

}