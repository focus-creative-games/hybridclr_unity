using HybridCLR.Editor.Template;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HybridCLR.Editor.ReversePInvokeWrap
{
    internal class ReversePInvokeWrapperGenerator
    {
        public void Generate(string template, int wrapperCount, string outputFile)
        {

            var frr = new FileRegionReplace(template);
            var codes = new List<string>();

            codes.Add(@"
	void CallLuaFunction(void* xState, int32_t wrapperIndex)
	{
		const MethodInfo* method = MetadataModule::GetMethodInfoByReversePInvokeWrapperIndex(wrapperIndex);
		typedef void (*Callback)(void* xState, const MethodInfo* method);
		((Callback)GetInterpreterDirectlyCallMethodPointer(method))(xState, method);
	}
");

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
