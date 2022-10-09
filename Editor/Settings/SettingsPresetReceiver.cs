using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;

namespace HybridCLR.Editor
{
    public class SettingsPresetReceiver : PresetSelectorReceiver
    {
        private Object m_Target;
        private Preset m_InitialValue;

        internal void Init(Object target)
        {
            m_Target = target;
            m_InitialValue = new Preset(target);
        }
        public override void OnSelectionChanged(Preset selection)
        {
            if (selection != null)
            {
                Undo.RecordObject(m_Target, "Apply Preset " + selection.name);
                selection.ApplyTo(m_Target);
            }
            else
            {
                Undo.RecordObject(m_Target, "Cancel Preset");
                m_InitialValue.ApplyTo(m_Target);
            }
#if UNITY_2020_1_OR_NEWER
            SettingsService.RepaintAllSettingsWindow();
#endif
        }
        public override void OnSelectionClosed(Preset selection)
        {
            OnSelectionChanged(selection);
            Object.DestroyImmediate(this);
        }
    }
}