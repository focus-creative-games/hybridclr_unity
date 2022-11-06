using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HybridCLR
{
    public static class RuntimeApi
    {
#if UNITY_STANDALONE_WIN
        private const string dllName = "GameAssembly";
#elif UNITY_IOS || UNITY_STANDALONE_OSX || UNITY_WEBGL
    private const string dllName = "__Internal";
#else
    private const string dllName = "il2cpp";
#endif

        /// <summary>
        /// 加载补充元数据assembly
        /// </summary>
        /// <param name="dllBytes"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public static unsafe LoadImageErrorCode LoadMetadataForAOTAssembly(byte[] dllBytes, HomologousImageMode mode)
        {
#if UNITY_EDITOR
            return LoadImageErrorCode.OK;
#else
            fixed(byte* data = dllBytes)
            {
                return (LoadImageErrorCode)LoadMetadataForAOTAssembly(data, dllBytes.Length, (int)mode);
            }
#endif
        }

        /// <summary>
        /// 加载补充元数据assembly
        /// </summary>
        /// <param name="dllBytes"></param>
        /// <param name="dllSize"></param>
        /// <returns></returns>
        [DllImport(dllName, EntryPoint = "RuntimeApi_LoadMetadataForAOTAssembly")]
        public static extern unsafe int LoadMetadataForAOTAssembly(byte* dllBytes, int dllSize, int mode);


        //[DllImport(dllName, EntryPoint = "RuntimeApi_UseDifferentialHybridAOTAssembly")]
        //public static unsafe LoadImageErrorCode UseDifferentialHybridAOTAssembly(string assemblyName)
        //{
        //    byte[] nameBytes = System.Text.Encoding.UTF8.GetBytes(assemblyName);
        //    fixed(byte* namePtr = nameBytes)
        //    {

        //    }
        //}

        /// <summary>
        /// 指示混合执行assembly使用原始的AOT代码。当assembly没有发生变化时必须调用此接口。
        /// </summary>
        /// <param name="assemblyName"></param>
        /// <returns></returns>
#if UNITY_EDITOR
        public static LoadImageErrorCode UseDifferentialHybridAOTAssembly(string assemblyName)
        {
            return LoadImageErrorCode.OK;
        }
#else
        [DllImport(dllName, EntryPoint = "RuntimeApi_UseDifferentialHybridAOTAssembly")]
        public static extern LoadImageErrorCode UseDifferentialHybridAOTAssembly(string assemblyName);
#endif


        /// <summary>
        /// 加载差分混合执行 assembly
        /// </summary>
        /// <param name="dllBytes"></param>
        /// <param name="optionBytes"></param>
        /// <returns></returns>
        public static unsafe LoadImageErrorCode LoadDifferentialHybridAssembly(byte[] dllBytes, byte[] optionBytes)
        {
#if UNITY_EDITOR
            return LoadImageErrorCode.OK;
#else
            fixed(byte* dllBytesPtr = dllBytes)
            {
                fixed(byte* optionBytesPtr = optionBytes)
                {
                    return (LoadImageErrorCode)LoadDifferentialHybridAssembly(dllBytesPtr, dllBytes.Length, optionBytesPtr, optionBytes.Length);
                }
            }
#endif
        }

        [DllImport(dllName, EntryPoint = "RuntimeApi_LoadDifferentialHybridAssembly")]
        public static extern unsafe int LoadDifferentialHybridAssembly(byte* dllBytes, int dllSize, byte* optionBytesPtr, int optionBytesLength);

        /// <summary>
        /// 获取解释器线程栈的最大StackObject个数(size*8 为最终占用的内存大小)
        /// </summary>
        /// <returns></returns>
        [DllImport(dllName, EntryPoint = "RuntimeApi_GetInterpreterThreadObjectStackSize")]
        public static extern int GetInterpreterThreadObjectStackSize();

        /// <summary>
        /// 设置解释器线程栈的最大StackObject个数(size*8 为最终占用的内存大小)
        /// </summary>
        /// <param name="size"></param>
        [DllImport(dllName, EntryPoint = "RuntimeApi_SetInterpreterThreadObjectStackSize")]
        public static extern void SetInterpreterThreadObjectStackSize(int size);

        /// <summary>
        /// 获取解释器线程函数帧数量(sizeof(InterpreterFrame)*size 为最终占用的内存大小)
        /// </summary>
        /// <returns></returns>
        [DllImport(dllName, EntryPoint = "RuntimeApi_GetInterpreterThreadFrameStackSize")]
        public static extern int GetInterpreterThreadFrameStackSize();

        /// <summary>
        /// 设置解释器线程函数帧数量(sizeof(InterpreterFrame)*size 为最终占用的内存大小)
        /// </summary>
        /// <param name="size"></param>
        [DllImport(dllName, EntryPoint = "RuntimeApi_SetInterpreterThreadFrameStackSize")]
        public static extern void SetInterpreterThreadFrameStackSize(int size);
    }
}
