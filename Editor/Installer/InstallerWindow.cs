using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;


namespace HybridCLR.Editor.Installer
{
    public class InstallerWindow : EditorWindow
    {
        private InstallerController m_Controller;

        string _hybridclrVersion;
        string _il2cppPlusVersion;

        private void OnEnable()
        {
            m_Controller = new InstallerController();
            _hybridclrVersion = "1.0";
            _il2cppPlusVersion = $"{m_Controller.Il2CppBranch}-1.0";
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

            string il2cppBranch = m_Controller.Il2CppBranch;
            string minCompatibleVersion = m_Controller.GetMinCompatibleVersion(il2cppBranch);
            GUI.enabled = true;
            GUILayout.Space(10f);

            EditorGUILayout.BeginVertical("box");
            bool hasInstall = m_Controller.HasInstalledHybridCLR();
            EditorGUILayout.LabelField($"安装状态：{(hasInstall ? "已安装" : "未安装")}", EditorStyles.boldLabel);

            string hybridclrLocalVersion = m_Controller.HybridclrLocalVersion;
            string il2cppPlusLocalVersion = m_Controller.Il2cppPlusLocalVersion;
            if (hasInstall)
            {
                EditorGUILayout.LabelField($"HybridCLR 版本:    {hybridclrLocalVersion}");
                GUILayout.Space(5f);
                EditorGUILayout.LabelField($"il2cpp_plus 版本:    {il2cppPlusLocalVersion}");
                GUILayout.Space(5f);
                //GUIInstallButton("检查更新", "检查", UpdateHybridCLR);
                //GUILayout.Space(40f);
            }
            
            GUISelectUnityDirectory($"il2cpp_plus分支对应的Unity兼容版本的il2cpp路径", "Select");
            GUILayout.Space(10f);


            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("待安装的 hybridclr 仓库版本号(或branch或tag)(默认取最新版本):", GUILayout.MaxWidth(400));
            _hybridclrVersion = EditorGUILayout.TextField(_hybridclrVersion);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"待安装的 il2cpp_plus 仓库版本号(或branch或tag)(默认取{il2cppBranch}-main分支最新版本):", GUILayout.MaxWidth(400));
            _il2cppPlusVersion = EditorGUILayout.TextField(_il2cppPlusVersion);
            EditorGUILayout.EndHorizontal();


            GUIInstallButton("安装hybridclr+il2cpp_plus代码到本地目录", "安装", InitHybridCLR);
            EditorGUILayout.EndVertical();
        }

        private void GUIInstallButton(string content, string button, Action onClick)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(content);
            GUI.enabled = m_Controller.CheckValidIl2CppInstallDirectory(m_Controller.Il2CppBranch, m_Controller.Il2CppInstallDirectory) == InstallErrorCode.Ok;
            if (GUILayout.Button(button, GUILayout.Width(100)))
            {
                onClick?.Invoke();
                GUIUtility.ExitGUI();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

        }


        private void GUISelectUnityDirectory(string content, string selectButton)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(content, GUILayout.MaxWidth(300));
            string il2cppInstallDirectory = m_Controller.Il2CppInstallDirectory = EditorGUILayout.TextField(m_Controller.Il2CppInstallDirectory);
            if (GUILayout.Button(selectButton, GUILayout.Width(100)))
            {
                string temp = EditorUtility.OpenFolderPanel(content, m_Controller.Il2CppInstallDirectory, string.Empty);
                if (!string.IsNullOrEmpty(temp))
                {
                    il2cppInstallDirectory = m_Controller.Il2CppInstallDirectory = temp;
                }
            }
            EditorGUILayout.EndHorizontal();

            InstallErrorCode err = m_Controller.CheckValidIl2CppInstallDirectory(m_Controller.Il2CppBranch, il2cppInstallDirectory);
            switch (err)
            {
                case InstallErrorCode.Ok:
                    {
                        break;
                    }
                case InstallErrorCode.Il2CppInstallPathNotExists:
                    {
                        EditorGUILayout.HelpBox("li2cpp 路径不存在", MessageType.Error);
                        break;
                    }
                case InstallErrorCode.InvalidUnityInstallPath:
                    {
                        EditorGUILayout.HelpBox($"Unity安装目录必须包含版本号，否则无法识别版本", MessageType.Error);
                        break;
                    }
                case InstallErrorCode.Il2CppInstallPathNotMatchIl2CppBranch:
                    {
                        EditorGUILayout.HelpBox($"il2cpp 版本不兼容，最小版本为 {m_Controller.GetMinCompatibleVersion(m_Controller.Il2CppBranch)}", MessageType.Error);
                        break;
                    }
                case InstallErrorCode.NotIl2CppPath:
                    {
                        EditorGUILayout.HelpBox($"当前选择的路径不是il2cpp目录（必须类似 xxx/il2cpp）", MessageType.Error);
                        break;
                    }
                default: throw new Exception($"not support {err}");
            }
        }

        private void InitHybridCLR()
        {
            string hybridclrVersion = string.IsNullOrWhiteSpace(_hybridclrVersion) ? "1.0" : _hybridclrVersion;
            string il2cppPlusVersion = string.IsNullOrWhiteSpace(_il2cppPlusVersion) ? $"{m_Controller.Il2CppBranch}-1.0" : _il2cppPlusVersion;
            m_Controller.InitHybridCLR(m_Controller.Il2CppBranch, m_Controller.Il2CppInstallDirectory, hybridclrVersion, il2cppPlusVersion);
        }

        private void UpdateHybridCLR()
        {
            bool hasUpdateIl2Cpp = m_Controller.HasUpdateIl2Cpp(m_Controller.Il2CppBranch);
            if (hasUpdateIl2Cpp)
            {
                bool ret = EditorUtility.DisplayDialog("检查更新", "版本不一致", "更新","取消");
                if (ret)
                {
                    InitHybridCLR();
                }
            }
            else
            {
                EditorUtility.DisplayDialog("检查更新", "暂无更新", "确定");    
            }
        }
    }
}
