using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;

namespace HybridCLR.Editor.Commands
{
    public static class PrebuildCommand
    {
        /// <summary>
        /// 按照必要的顺序，执行所有生成操作，适合打包前操作
        /// </summary>
        [MenuItem("HybridCLR/GenerateAll", priority = 30)]
        public static void GenerateAll()
        {
            // 顺序随意
            ReversePInvokeWrapperGeneratorCommand.GenerateReversePInvokeWrapper();

            // AOTReferenceGeneratorCommand 涉及到代码生成，必须在MethodBridgeGeneratorCommand之前
            AOTReferenceGeneratorCommand.GenerateAOTGenericReference();
            MethodBridgeGeneratorCommand.GenerateMethodBridge();

            // 顺序随意，只要保证 GenerateLinkXml之前有调用过CompileDll即可
            LinkGeneratorCommand.GenerateLinkXml(false);
        }
    }
}
