using HybridCLR.Editor;
using System;
using UnityEditor;
using UnityEditorInternal;

/// <summary>
/// 监听编辑器状态，当编辑器重新 focus 时，重新加载实例，避免某些情景下 svn 、git 等外部修改了数据却无法同步的异常。
/// </summary>
[InitializeOnLoad]
public static class EditorStatusWatcher
{
    public static Action OnEditorFocused;
    static bool isFocused;
    static EditorStatusWatcher() => EditorApplication.update += Update;
    static void Update()
    {
        //当编辑器 focus 后如果优先发生编译，则等待其完成编译再刷新内部逻辑（猜想&防御）
        if (!EditorApplication.isCompiling)
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