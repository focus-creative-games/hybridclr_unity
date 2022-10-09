using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;
using UnityEngine.UIElements;
namespace HybridCLR.Editor
{
    public class HybridCLRSettingsProvider : SettingsProvider
    {
        private static SerializedObject m_SerializedObject;
        private SerializedProperty m_Enable;
        private SerializedProperty m_UseGlobalIl2cpp;
        private SerializedProperty m_CloneFromGitee;
        private SerializedProperty m_HotUpdateAssemblyDefinitions;
        private SerializedProperty m_HotUpdateAssemblies;
        private SerializedProperty m_OutputLinkFile;
        private SerializedProperty m_OutputAOTGenericReferenceFile;
        private SerializedProperty m_MaxGenericReferenceIteration;
        private SerializedProperty m_ReversePInvokeWrapperCount;
        private SerializedProperty m_MaxMethodBridgeGenericIteration;
        private GUIStyle buttonStyle;
        public HybridCLRSettingsProvider() : base("Project/HybridCLR Settings", SettingsScope.Project) { }
        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            HybridCLRSettings.Instance.Save();
            var setting = HybridCLRSettings.Instance;
            setting.hideFlags &= ~HideFlags.NotEditable;
            m_SerializedObject = m_SerializedObject ?? new SerializedObject(setting);
            m_Enable = m_SerializedObject.FindProperty("enable");
            m_UseGlobalIl2cpp = m_SerializedObject.FindProperty("useGlobalIl2cpp");
            m_CloneFromGitee = m_SerializedObject.FindProperty("cloneFromGitee");
            m_HotUpdateAssemblyDefinitions = m_SerializedObject.FindProperty("hotUpdateAssemblyDefinitions");
            m_HotUpdateAssemblies = m_SerializedObject.FindProperty("hotUpdateAssemblies");
            m_OutputLinkFile = m_SerializedObject.FindProperty("outputLinkFile");
            m_OutputAOTGenericReferenceFile = m_SerializedObject.FindProperty("outputAOTGenericReferenceFile");
            m_MaxGenericReferenceIteration = m_SerializedObject.FindProperty("maxGenericReferenceIteration");
            m_ReversePInvokeWrapperCount = m_SerializedObject.FindProperty("ReversePInvokeWrapperCount");
            m_MaxMethodBridgeGenericIteration = m_SerializedObject.FindProperty("maxMethodBridgeGenericIteration");
        }
        public override void OnTitleBarGUI()
        {
            base.OnTitleBarGUI();
            var rect = GUILayoutUtility.GetLastRect();
            buttonStyle = buttonStyle ?? GUI.skin.GetStyle("IconButton");

            #region  绘制官方网站跳转按钮
            var w = rect.x + rect.width;
            rect.x = w - 58;
            rect.y += 6;
            rect.width = rect.height = 18;
            var content = EditorGUIUtility.IconContent("_Help");
            content.tooltip = "点击访问 HybridCLR 官方文档";
            if (GUI.Button(rect, content, buttonStyle))
            {
                Application.OpenURL("https://focus-creative-games.github.io/hybridclr/");
            }
            #endregion

            #region 绘制 Preset
            rect.x += 19;
            content = EditorGUIUtility.IconContent("Preset.Context");
            content.tooltip = "点击存储或加载 Preset .";
            if (GUI.Button(rect, content, buttonStyle))
            {
                var target = HybridCLRSettings.Instance;
                var receiver = ScriptableObject.CreateInstance<SettingsPresetReceiver>();
                receiver.Init(target);
                PresetSelector.ShowSelector(target, null, true, receiver);
            }
            #endregion
            #region 绘制 Reset
            rect.x += 19;
            content = EditorGUIUtility.IconContent("_Popup");
            content.tooltip = "Reset";
            if (GUI.Button(rect, content, buttonStyle))
            {
                GenericMenu menu = new GenericMenu(); 
                menu.AddItem(new GUIContent("Reset"), false, () => 
                {
                    Undo.RecordObject(HybridCLRSettings.Instance, "Capture Value for Reset");
                    var dv = ScriptableObject.CreateInstance<HybridCLRSettings>();
                    var json = EditorJsonUtility.ToJson(dv);
                    EditorJsonUtility.FromJsonOverwrite(json,HybridCLRSettings.Instance);
                    HybridCLRSettings.Instance.Save();
                });
                menu.ShowAsContext(); 
            }
            #endregion
        }
        public override void OnGUI(string searchContext)
        {
            using (CreateSettingsWindowGUIScope())
            {
                if (m_SerializedObject == null || !m_SerializedObject.targetObject)
                {
                    m_SerializedObject = null;
                    m_SerializedObject = new SerializedObject(HybridCLRSettings.Instance);
                }
                m_SerializedObject.Update();
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_Enable);
                EditorGUILayout.PropertyField(m_CloneFromGitee);
                EditorGUILayout.PropertyField(m_UseGlobalIl2cpp);
                EditorGUILayout.PropertyField(m_HotUpdateAssemblyDefinitions);
                EditorGUILayout.PropertyField(m_HotUpdateAssemblies);
                EditorGUILayout.PropertyField(m_OutputLinkFile);
                EditorGUILayout.PropertyField(m_OutputAOTGenericReferenceFile);
                EditorGUILayout.PropertyField(m_MaxGenericReferenceIteration);
                EditorGUILayout.PropertyField(m_ReversePInvokeWrapperCount);
                EditorGUILayout.PropertyField(m_MaxMethodBridgeGenericIteration);
                if (EditorGUI.EndChangeCheck())
                {
                    m_SerializedObject.ApplyModifiedProperties();
                    HybridCLRSettings.Instance.Save();
                }
            }
        }
        private IDisposable CreateSettingsWindowGUIScope()
        {
            var unityEditorAssembly = Assembly.GetAssembly(typeof(EditorWindow));
            var type = unityEditorAssembly.GetType("UnityEditor.SettingsWindow+GUIScope");
            return Activator.CreateInstance(type) as IDisposable;
        }
        public override void OnDeactivate()
        {
            base.OnDeactivate();
            HybridCLRSettings.Instance.Save();
            m_SerializedObject = null;
        }
        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            if (HybridCLRSettings.Instance)
            {
                var provider = new HybridCLRSettingsProvider
                {
                    keywords = GetSearchKeywordsFromSerializedObject(m_SerializedObject = m_SerializedObject ?? new SerializedObject(HybridCLRSettings.Instance))
                };
                return provider;
            }
            return null;
        }
    }
}