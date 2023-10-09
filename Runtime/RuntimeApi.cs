using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HybridCLR
{
    public static class RuntimeApi
    {
#if UNITY_EDITOR
        private static int s_interpreterThreadObjectStackSize = 128 * 1024;
        private static int s_interpreterThreadFrameStackSize = 2 * 1024;
#endif

        /// <summary>
        /// 加载补充元数据assembly
        /// </summary>
        /// <param name="dllBytes"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
#if UNITY_EDITOR
        public static unsafe LoadImageErrorCode LoadMetadataForAOTAssembly(byte[] dllBytes, HomologousImageMode mode)
        {
            return LoadImageErrorCode.OK;
        }
#else
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern LoadImageErrorCode LoadMetadataForAOTAssembly(byte[] dllBytes, HomologousImageMode mode);
#endif

        /// <summary>
        /// 获取解释器线程栈的最大StackObject个数(size*8 为最终占用的内存大小)
        /// </summary>
        /// <returns></returns>
#if UNITY_EDITOR
        public static int GetInterpreterThreadObjectStackSize()
        {
            return s_interpreterThreadObjectStackSize;
        }
#else
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern int GetInterpreterThreadObjectStackSize();
#endif

        /// <summary>
        /// 设置解释器线程栈的最大StackObject个数(size*8 为最终占用的内存大小)
        /// </summary>
        /// <param name="size"></param>
#if UNITY_EDITOR
        public static void SetInterpreterThreadObjectStackSize(int size)
        {
            s_interpreterThreadObjectStackSize = size;
        }
#else
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern void SetInterpreterThreadObjectStackSize(int size);
#endif
        /// <summary>
        /// 获取解释器线程函数帧数量(sizeof(InterpreterFrame)*size 为最终占用的内存大小)
        /// </summary>
        /// <returns></returns>
#if UNITY_EDITOR
        public static int GetInterpreterThreadFrameStackSize()
        {
            return s_interpreterThreadFrameStackSize;
        }
#else
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern int GetInterpreterThreadFrameStackSize();
#endif
        
        /// <summary>
        /// 设置解释器线程函数帧数量(sizeof(InterpreterFrame)*size 为最终占用的内存大小)
        /// </summary>
        /// <param name="size"></param>
#if UNITY_EDITOR
        public static void SetInterpreterThreadFrameStackSize(int size)
        {
            s_interpreterThreadFrameStackSize = size;
        }
#else
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern void SetInterpreterThreadFrameStackSize(int size);
#endif
    }
}
