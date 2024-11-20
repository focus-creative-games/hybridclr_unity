using HybridCLR.Editor;
using System;
using UnityEditor;
using UnityEditorInternal;

namespace HybridCLR.Editor.Settings
{

    [InitializeOnLoad]
    public static class EditorStatusWatcher
    {
        public static Action OnEditorFocused;
        static bool isFocused;
        static EditorStatusWatcher() => EditorApplication.update += Update;
        static void Update()
        {
            if (isFocused != InternalEditorUtility.isApplicationActive)
            {
                isFocused = InternalEditorUtility.isApplicationActive;
                if (isFocused)
                {
                    HybridCLRSettings.LoadOrCreate();
                    OnEditorFocused?.Invoke();
                }
            }
        }
    }

}