using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace HybridCLR.Editor.Installer
{
    public class InstallerWindow : EditorWindow
    {
        private InstallerController _controller;
        private VersionResolver resolver_h; //for hybridclr tag based update
        private VersionResolver resolver_i;  //for il2cpp_plus tag based update
        private GUIStyle style_label;

        private void OnEnable()
        {
            _controller = new InstallerController(this);
            resolver_h = new VersionResolver(Repository.HybridCLR, this);
            resolver_i = new VersionResolver(Repository.IL2CPP_Plus, this, branch: SettingsUtil.UnityMajorVersion);
        }

        private void OnGUI()
        {
            using (var v = new EditorGUILayout.VerticalScope("box"))
            {
                bool hasInstall = InstallerController.HasInstalledHybridCLR;
                EditorGUILayout.LabelField($"安装状态：{(hasInstall ? "已安装" : "未安装")}", EditorStyles.boldLabel);
                GUILayout.Space(10f);
                if (hasInstall)
                {
                    style_label = style_label ?? new GUIStyle(EditorStyles.label) { richText = true };
                    EditorGUILayout.LabelField($"HybridCLR 版本:    {resolver_h.current}   {(resolver_h.HasUpdate ? "<color=green>(有更新)</color>" : "")}", style_label);
                    GUILayout.Space(5f);
                    EditorGUILayout.LabelField($"il2cpp_plus 版本:    {resolver_i.current}   {(resolver_i.HasUpdate ? "<color=green>(有更新)</color>" : "")}", style_label);
                }

                // 绘制 HybridCLR 版本选择下拉框
                GUILayout.Space(4f);
                EditorGUILayout.LabelField("HybridCLR Version:", GUILayout.Width(120));
                resolver_h.DrawVersionSelector();

                // 绘制 il2cpp_plus 版本选择下拉框
                GUILayout.Space(4f);
                EditorGUILayout.LabelField("il2cpp_plus Version:", GUILayout.Width(120));
                resolver_i.DrawVersionSelector();
                
                DrawUpdateCheckButton(v.rect);
                GUILayout.Space(20f);
                GUIInstallButton("安装hybridclr+il2cpp_plus代码到本地目录", "安装", InitHybridCLR);
            }
            DrawSettingsButton();
        }

        private static void DrawSettingsButton()
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
        }

        private void DrawUpdateCheckButton(Rect source)
        {
            var rect = new Rect(source);
            rect.x = rect.width - 99;
            rect.width = 100;
            rect.y = rect.height - 40;
            rect.height = EditorGUIUtility.singleLineHeight;
            GUI.enabled = resolver_h.IsValid && resolver_i.IsValid;
            if (GUI.Button(rect, "检查更新"))
            {
                Task.Run(UpdateHybridCLR);
                GUIUtility.ExitGUI();
            }
            GUI.enabled = true;
        }

        private void GUIInstallButton(string content, string button, Action onClick)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(content);
            GUI.enabled = resolver_h.IsValid && resolver_i.IsValid;
            if (GUILayout.Button(button, GUILayout.Width(100)))
            {
                InitHybridCLR();
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();
        }


        private void InitHybridCLR()
        {
            if (!_controller.IsComaptibleVersion())
            {
                Debug.LogError($"il2cpp 版本不兼容，最小版本为 {_controller.GetCurrentUnityVersionMinCompatibleVersionStr()}");
                return;
            }
            _controller.InitHybridCLR(SettingsUtil.UnityMajorVersion, resolver_h, resolver_i);
        }

        private async void UpdateHybridCLR()
        {
            var result_hybridclr = resolver_h.CheckUpdateAsync();
            var result_il2cpp_plus = resolver_i.CheckUpdateAsync();
            await Task.WhenAll(result_hybridclr, result_il2cpp_plus);
            Loom.Post(() =>
            {
                Repaint();
                if (InstallerController.HasInstalledHybridCLR)
                {
                    var result_h = result_hybridclr.Result;
                    var result_i = result_il2cpp_plus.Result;
                    if (result_h.hasUpdate || result_i.hasUpdate)
                    {
                        var msg = $"检查到更新，是否更新？{(result_i.hasUpdate ? $"\nil2cpp_plus: {result_i.tag}" : "")}{(result_h.hasUpdate ? $"\nHybridCLR: {result_h.tag}" : "")}";
                        if (EditorUtility.DisplayDialog("检查到更新", msg, "更新", "取消"))
                        {
                            if (result_i.hasUpdate)
                            {
                                resolver_i.CheckoutSpecificTag(result_i.tag);
                            }
                            if (result_h.hasUpdate)
                            {
                                resolver_h.CheckoutSpecificTag(result_h.tag);
                            }
                            InstallerController.DuplicateRepoData();
                        }
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("检查更新", "当前已经是最新版本", "确定");
                    }
                }
                else
                {
                    ShowNotification(new GUIContent("版本信息同步完成！"));
                }
            });
        }
    }
}
