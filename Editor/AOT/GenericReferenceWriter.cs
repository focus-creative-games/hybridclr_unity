using HybridCLR.Editor.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace HybridCLR.Editor.AOT
{
    public class GenericReferenceWriter
    {
        public void Write(List<GenericClass> types, List<GenericMethod> methods, string outputFile)
        {
            string parentDir = Directory.GetParent(outputFile).FullName;
            Directory.CreateDirectory(parentDir);

            List<string> codes = new List<string>();
            codes.Add("public class AOTGenericReferences : UnityEngine.MonoBehaviour");
            codes.Add("{");


            foreach(var type in types)
            {
                codes.Add($"\t//{type.Type}");
            }

            codes.Add("\tpublic void RefMethods()");
            codes.Add("\t{");

            foreach(var method in methods)
            {
                codes.Add($"\t\t// {method.Method}");
            }
            codes.Add("\t}");

            codes.Add("}");


            var utf8WithoutBOM = new System.Text.UTF8Encoding(false);
            File.WriteAllText(outputFile, string.Join("\n", codes), utf8WithoutBOM);
            Debug.Log($"[GenericReferenceWriter] write {outputFile}");
        }
    }
}
