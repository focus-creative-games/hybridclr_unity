using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;


namespace HybridCLR.Editor.Installer
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
            content.tooltip = "点击打开HybridCLR Settings";
            if (GUI.Button(rect, content, GUI.skin.GetStyle("IconButton")))
            {
                SettingsService.OpenProjectSettings("Project/HybridCLR Settings");
            }

            bool hasInstall = _controller.HasInstalledHybridCLR();

            GUILayout.Space(10f);

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"安装状态：{(hasInstall ? "已安装" : "未安装")}", EditorStyles.boldLabel);
            GUILayout.Space(10f);

            EditorGUILayout.LabelField($"HybridCLR 版本:    {_controller.HybridclrLocalVersion}");
            GUILayout.Space(5f);
            EditorGUILayout.LabelField($"il2cpp_plus 版本:    {_controller.Il2cppPlusLocalVersion}");
            GUILayout.Space(5f);
            //GUIInstallButton("检查更新", "检查", UpdateHybridCLR);
            //GUILayout.Space(40f);
            
            GUILayout.Space(10f);


            //EditorGUILayout.BeginHorizontal();
            //EditorGUILayout.LabelField("待安装的 hybridclr 仓库版本号(或branch或tag)(默认取最新版本):", GUILayout.MaxWidth(400));
            //_hybridclrVersion = EditorGUILayout.TextField(_hybridclrVersion);
            //EditorGUILayout.EndHorizontal();

            //EditorGUILayout.BeginHorizontal();
            //EditorGUILayout.LabelField($"待安装的 il2cpp_plus 仓库版本号(或branch或tag)(默认取{_controller.MajorVersion}-main分支最新版本):", GUILayout.MaxWidth(400));
            //_il2cppPlusVersion = EditorGUILayout.TextField(_il2cppPlusVersion);
            //EditorGUILayout.EndHorizontal();


            GUIInstallButton("安装hybridclr+il2cpp_plus代码到本地目录", "安装");
            EditorGUILayout.EndVertical();
        }

        private void GUIInstallButton(string content, string button)
        {
            EditorGUILayout.BeginHorizontal();
            _installFromDir = EditorGUILayout.Toggle("从本地复制libil2cpp", _installFromDir);
            EditorGUI.BeginDisabledGroup(!_installFromDir);
            EditorGUILayout.TextField(_installLibil2cppWithHybridclrSourceDir, GUILayout.Width(400));
            if (GUILayout.Button("选择目录", GUILayout.Width(100)))
            {
                _installLibil2cppWithHybridclrSourceDir = EditorUtility.OpenFolderPanel("选择libil2cpp目录", Application.dataPath, "libil2cpp");
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(content);
            if (GUILayout.Button(button, GUILayout.Width(100)))
            {
                InstallLocalHybridCLR();
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();

        }

        private void InstallLocalHybridCLR()
        {
            if (_installFromDir)
            {
                if (!Directory.Exists(_installLibil2cppWithHybridclrSourceDir))
                {
                    Debug.LogError($"本地libil2cpp复制目录不存在. '{_installLibil2cppWithHybridclrSourceDir}'");
                    return;
                }
                if (!File.Exists($"{_installLibil2cppWithHybridclrSourceDir}/il2cpp-config.h") || !File.Exists($"{_installLibil2cppWithHybridclrSourceDir}/hybridclr/RuntimeApi.cpp"))
                {
                    Debug.LogError($"本地libil2cpp不是合法有效的源码目录. '{_installLibil2cppWithHybridclrSourceDir}'");
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
