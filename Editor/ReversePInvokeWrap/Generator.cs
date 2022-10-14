﻿using HybridCLR.Editor.Template;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HybridCLR.Editor.ReversePInvokeWrap
{
    public class Generator
    {
        public void Generate(string template, int wrapperCount, string outputFile)
        {

            var frr = new FileRegionReplace(template);
            var codes = new List<string>();

            for(int i = 0; i < wrapperCount; i++)
            {
                codes.Add($@"
	void __ReversePInvokeMethod_{i}(void* xState)
	{{
		CallLuaFunction(xState, {i});
	}}
");
            }

            codes.Add(@"
    Il2CppMethodPointer s_ReversePInvokeMethodStub[]
	{
");
            for(int i = 0;  i < wrapperCount; i++)
            {
                codes.Add($"\t\t(Il2CppMethodPointer)__ReversePInvokeMethod_{i},\n");
            }

            codes.Add(@"
		nullptr,
	};
");

            frr.Replace("REVERSE_PINVOKE_METHOD_STUB", string.Join("", codes));
            frr.Commit(outputFile);
        }
    }
}
