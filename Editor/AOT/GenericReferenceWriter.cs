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

            codes.Add("");
            codes.Add("\t// {{ constraint implement type");

            codes.Add("\t// }} ");

            codes.Add("");
            codes.Add("\t// {{ AOT generic type");

            types.Sort((a, b) => a.Type.FullName.CompareTo(b.Type.FullName));
            foreach(var type in types)
            {
                codes.Add($"\t//{type.ToTypeSig()}");
            }

            codes.Add("\t// }}");

            codes.Add("");
            codes.Add("\tpublic void RefMethods()");
            codes.Add("\t{");
            methods.Sort((a, b) =>
            {
                int c = a.Method.DeclaringType.FullName.CompareTo(b.Method.DeclaringType.FullName);
                if (c != 0)
                {
                    return c;
                }
                c = a.Method.Name.CompareTo(b.Method.Name);
                return c;
            });
            foreach(var method in methods)
            {
                codes.Add($"\t\t// {method.ToMethodSpec()}");
            }
            codes.Add("\t}");

            codes.Add("}");


            var utf8WithoutBOM = new System.Text.UTF8Encoding(false);
            File.WriteAllText(outputFile, string.Join("\n", codes), utf8WithoutBOM);
            Debug.Log($"[GenericReferenceWriter] write {outputFile}");
        }
    }
}
