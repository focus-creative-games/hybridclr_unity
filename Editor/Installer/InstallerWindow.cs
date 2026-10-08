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

using System.IO;
using UnityEditor;
using UnityEngine;


namespace HybridCLR.Installer
{
    public class InstallerWindow : EditorWindow
    {
        private InstallerController _controller;

        private bool _installFromDir;

        private string _installLibil2cppWithHybridclrSourceDir;

        private void OnEnable()
        {
            _controller = new InstallerController();
        }

        private void OnGUI()
        {
            var rect = new Rect
            {
                x = EditorGUIUtility.currentViewWidth - 24,
                y = 5,
                width = 24,
                height = 24
            };
            var content = EditorGUIUtility.IconContent("Settings");
            content.tooltip = "HybridCLR Settings";
            if (GUI.Button(rect, content, GUI.skin.GetStyle("IconButton")))
            {
                SettingsService.OpenProjectSettings("Project/HybridCLR Settings");
            }

            bool hasInstall = _controller.HasInstalledHybridCLR();

            GUILayout.Space(10f);

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"Installed: {hasInstall}", EditorStyles.boldLabel);
            GUILayout.Space(10f);

            EditorGUILayout.LabelField($"Package Version:   v{_controller.PackageVersion}");
            GUILayout.Space(5f);
            EditorGUILayout.LabelField($"Installed Version:    v{_controller.InstalledLibil2cppVersion ?? " Unknown"}");
            GUILayout.Space(5f);

            GUILayout.Space(10f);



            EditorGUILayout.BeginHorizontal();
            _installFromDir = EditorGUILayout.Toggle("Copy libil2cpp from local", _installFromDir, GUILayout.MinWidth(100));
            EditorGUI.BeginDisabledGroup(!_installFromDir);
            EditorGUILayout.TextField(_installLibil2cppWithHybridclrSourceDir, GUILayout.Width(400));
            if (GUILayout.Button("Choose", GUILayout.Width(100)))
            {
                _installLibil2cppWithHybridclrSourceDir = EditorUtility.OpenFolderPanel("Select libil2cpp", Application.dataPath, "libil2cpp");
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(20f);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Install", GUILayout.Width(100)))
            {
                InstallLocalHybridCLR();
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void InstallLocalHybridCLR()
        {
            if (_installFromDir)
            {
                if (!Directory.Exists(_installLibil2cppWithHybridclrSourceDir))
                {
                    Debug.LogError($"Source libil2cpp:'{_installLibil2cppWithHybridclrSourceDir}' doesn't exist.");
                    return;
                }
                if (!File.Exists($"{_installLibil2cppWithHybridclrSourceDir}/il2cpp-config.h") || !File.Exists($"{_installLibil2cppWithHybridclrSourceDir}/hybridclr/RuntimeApi.cpp"))
                {
                    Debug.LogError($"Source libil2cpp:' {_installLibil2cppWithHybridclrSourceDir} ' is invalid");
                    return;
                }
                _controller.InstallFromLocal(_installLibil2cppWithHybridclrSourceDir);
            }
            else
            {
                _controller.InstallDefaultHybridCLR();
            }
        }
    }
}
