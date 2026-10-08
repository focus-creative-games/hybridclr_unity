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

using UnityEditor;
using UnityEngine.UIElements;

namespace HybridCLR.Settings
{
    public class HybridCLRSettingsProvider : SettingsProvider
    {
        private static HybridCLRSettingsProvider s_provider;

        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            if (s_provider == null)
            {
                s_provider = new HybridCLRSettingsProvider();
            }

            return s_provider;
        }

        private SerializedObject _serializedObject;

        public HybridCLRSettingsProvider() : base("Project/HybridCLR Settings", SettingsScope.Project)
        {
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            InitGUI();
            using (var so = new SerializedObject(HybridCLRSettings.instance))
            {
                keywords = GetSearchKeywordsFromSerializedObject(so);
            }
        }

        public override void OnDeactivate()
        {
            base.OnDeactivate();
            HybridCLRSettings.Save();
        }

        private void InitGUI()
        {
            var setting = HybridCLRSettings.instance;
            _serializedObject?.Dispose();
            _serializedObject = new SerializedObject(setting);
        }

        public override void OnGUI(string searchContext)
        {
            if (_serializedObject == null || !_serializedObject.targetObject)
            {
                InitGUI();
            }

            _serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            using (var prop = _serializedObject.GetIterator())
            {
                if (prop.NextVisible(true))
                {
                    do
                    {
                        if (prop.name == "m_Script")
                        {
                            continue;
                        }

                        EditorGUILayout.PropertyField(prop, true);
                    }
                    while (prop.NextVisible(false));
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                _serializedObject.ApplyModifiedProperties();
                HybridCLRSettings.Save();
            }
        }
    }
}
