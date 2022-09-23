using HybridCLR.Editor.Link;
using HybridCLR.Editor.ReversePInvokeWrap;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace HybridCLR.Editor.Commands
{

    public static class ReversePInvokeWrapperGeneratorCommand
    {

        [MenuItem("HybridCLR/Generate/ReversePInvokeWrapper", priority = 103)]
        public static void GenerateReversePInvokeWrapper()
        {
            string ReversePInvokeWrapperStubFile = $"{SettingsUtil.LocalIl2CppDir}/libil2cpp/hybridclr/metadata/ReversePInvokeMethodStub.cpp";
            string wrapperTemplateStr = AssetDatabase.LoadAssetAtPath<TextAsset>($"{SettingsUtil.TemplatePathInPackage}/ReversePInvokeMethodStub.cpp.txt").text;
            int wrapperCount = SettingsUtil.GlobalSettings.ReversePInvokeWrapperCount;
            var generator = new Generator();
            generator.Generate(wrapperTemplateStr, wrapperCount,ReversePInvokeWrapperStubFile);
            Debug.Log($"GenerateReversePInvokeWrapper. wraperCount:{wrapperCount} output:{ReversePInvokeWrapperStubFile}");
            MethodBridgeGeneratorCommand.CleanIl2CppBuildCache();
        }
    }
}
