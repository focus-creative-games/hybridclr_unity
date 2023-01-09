using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEditor;
using UnityEngine;
namespace HybridCLR.Editor
{
    /// <summary>
    /// 线程间通信，仅供 Editor 使用
    /// </summary>
    public static class Loom
    {
        static SynchronizationContext context;
        static readonly ConcurrentQueue<Action> tasks = new ConcurrentQueue<Action>();

        //5. 确保编辑器下推送的事件也能被执行
        [InitializeOnLoadMethod]
        static void EditorForceUpdate()
        {
            context = SynchronizationContext.Current;
            EditorApplication.update -= ForceEditorPlayerLoopUpdate;
            EditorApplication.update += ForceEditorPlayerLoopUpdate;
            void ForceEditorPlayerLoopUpdate()
            {
                if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                {
                    // Not in Edit mode, don't interfere
                    return;
                }
                Update();
            }
        }

        //  将需要在主线程中执行的委托传递进来
        public static void Post(Action task)
        {
            if (SynchronizationContext.Current == context)
            {
                task?.Invoke();
            }
            else
            {
                tasks.Enqueue(task);
            }
        }

        static void Update()
        {
            while (tasks.TryDequeue(out var task))
            {
                try
                {
                    task?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.Log($"{nameof(Loom)}:  封送的任务执行过程中发现异常，请确认: {e}");
                }
            }
        }
    }
}