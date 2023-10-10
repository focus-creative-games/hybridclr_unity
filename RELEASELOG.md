# 发布日志

## 4.0.8

发布日期 2023.10.10.

### Runtime

- [fix] 修复计算值类型泛型桥接函数签名时，错误地将值类型泛型参数类型也换成签名，导致与Editor计算的签名不一致的bug
- [fix][refactor] RuntimeApi相关函数由PInvoke改为InternalCall，解决Android平台调用RuntimeApi时触发重新加载libil2cpp.a的问题

### Editor

- [refactor] RuntimeApi相关函数由PInvoke改为InternalCall
- [refactor] 调整HybridCLR.Editor模块一些不规范的命名空间

## 4.0.7

发布日期 2023.10.09.

### Runtime

- [fix] 修复initobj调用了CopyN，但CopyN未考虑对象的内存对齐的情况，在32位这种的平台可能发生未对齐访问异常的bug
- [fix] 修复计算未完全实例化的泛型函数的桥接函数签名时崩溃的bug
- [fix] 修复Il2cpp代码生成选项为faster(smaller)时，2021和2022版本GenericMethod::CreateMethodLocked的bug
- [remove] 移除所有array相关指令中index为int64_t的指令，简化代码
- [remove] 移除ldfld_xxx_ref系列指令

### Editor

- [fix] 修复生成桥接函数时，如果热更新程序集未包含任何代码直接引用了某个aot程序集，则没有为该aot程序集生成桥接函数，导致出现NotSupportNative2Managed异常的bug
- [fix] 修复mac下面路径过长导致拷贝文件失败的bug
- [fix] 修复发布PS5目标时未处理ScriptingAssemblies.json的bug
- [change] 打包时清空裁减aot dll目录

## 4.0.6

发布日期 2023.09.26.

### Runtime

- [fix] 修复2021和2022版本开启完全泛型共享后的bug
- [fix] 修复加载PlaceHolder Assembly后未增加assemblyVersion导致Assembly::GetAssemblies()错误地获得了旧程序集列表的bug

## 4.0.5

发布日期 2023.09.25.

### Runtime

- [fix] 修复Transform中未析构pendingFlows造成内存泄露的bug
- [fix] 修复多维数组SetMdArrElement未区分带ref与不带ref结构的bug
- [fix] 修复CpobjVarVAr_WriteBarrier_n_4未设置size的bug
- [fix] 修复计算interface成员函数slot时未考虑到static之类函数的bug
- [fix] 修复2022版本ExplicitLayout未设置layout.alignment，导致计算出size==0的bug
- [fix] 修复InterpreterInvoke在完全泛型共享时，class类型的methodPointer与virtualMethodPointer有可能不一致，导致失误对this指针+1的bug
- [fix] ldobj当T为byte之类size<4的类型时，未将数据展开为int的bug
- [fix] 修复CopySize未考虑到内存对齐的问题
- [opt] 优化stelem当元素为size较大的struct时统一当作含ref结构的问题
- [opt] TemporaryMemoryArena默认内存块大小由1M调整8K
- [opt] 将Image::Image中Assembly::GetAllAssemblies()换成Assembly::GetAllAssemblies(AssemblyVector&)，避免创建assembly快照而造成不必要的内存泄露

### Editor

- [fix] 修复StandaloneLinux平台DllImport的dllName和裁剪dll路径的错误
- [change] 对于小版本不兼容的Unity版本，不再禁止安装，而是提示警告
- [fix] 修复桥接函数计算中MetaUtil.ToShareTypeSig将Ptr和ByRef计算成IntPtr的bug，正确应该是UIntPtr

## 4.0.4

发布日期 2023.09.11。

### Runtime

- [new][platform] 彻底支持所有平台，包括UWP和PS5
- [fix][严重] 修复计算interpreter部分enum类型的桥接函数签名的bug
- [fix] 修复在某些平台下有编译错误的问题
- [fix] 修复转换STOBJ指令未正确处理增量式GC的bug
- [fix] [fix] 修复 StindVarVar_ref指令未正确设置WriteBarrier的bug
- [fix] 修复2020 GenericMethod::CreateMethodLocked调用vm::MetadataAllocGenericMethod()未持有s_GenericMethodMutex锁的线程安全问题

### Editor

- [fix] 修复AddLil2cppSourceCodeToXcodeproj2021OrOlder在Unity 2020下偶然同时包含了不同目录的两个ThreadPool.cpp文件导致出现编译错误的问题
- [fix] 修复不正确地从EditorUserBuildSettings.selectedBuildTargetGroup获得BuildGroupTarget的bug
- [fix] StripAOTDllCommand生成AOT dll时的BuildOption采用当前Player的设置，避免当打包开启development时，StripAOTDllCommand生成Release aot dll，而打包生成debug aot dll，产生补充元数据及桥接函数生成不匹配的严重错误
- [change] 为了更好地支持全平台，调整了RuntimeApi.cs中dllName的实现，默认取 __Internal
- [change] 为了更好地支持全平台，自2021起裁剪AOT dll全都通过MonoHook复制

## 4.0.3

发布日期 2023.08.31。

### Editor

- [fix] 修复桥接函数计算的bug

## 4.0.2

发布日期 2023.08.29。

### Runtime

- [fix][严重] 修复LdobjVarVar_ref指令的bug。此bug由增量式GC代码引入
- [fix] 修复未处理ResolveField获得的Field为nullptr时情形导致崩溃的bug
- [fix] 修复未正确处理AOT及interpreter interface中显式实现父接口函数的bug

## 4.0.1

发布日期 2023.08.28。

### Runtime

- [fix] 修复2020版本开启增量式GC后出现编译错误的问题

## 4.0.0

发布日期 2023.08.28。

### Runtime

- [new] 支持增量式GC
- [refactor] 重构桥接函数，彻底支持所有il2cpp支持的平台
- [opt] 大幅优化Native2Managed方向的传参

### Editor

- [change] 删除增量式GC选项检查
- [refactor] 重构桥接函数生成

## 3.4.2

发布日期 2023.08.14。

### Runtime

- [fix] 修复RawImage::LoadTables读取_4byteGUIDIndex的bug
- [version] 支持2022.3.7版本
- [version] 支持2021.3.29版本

### Editor

- [fix] 修复计算AOTGenericReference未考虑到泛型调用泛型的情况，导致少计算了泛型及补充元数据

## 3.4.1

发布日期 2023.07.31。

### Runtime

- [fix] 修复 InitializeRuntimeMetadata的内存可见性问题
- [fix] 修复CustomAttribute未正确处理父类NamedArg导致崩溃的bug
- [opt] 优化Transfrom Instinct指令的代码，从HashMap中快速查找而不是挨个匹配

### Editor

- [fix] 修复FilterHotFixAssemblies只对比程序集名尾部，导致有AOT的尾部与某个热更新程序集匹配时意外被过滤的bug
- [change] 检查Settings中热更新程序集列表配置中程序集名不能为空

## 3.4.0

发布日期 2023.07.17。

### Runtime

- [version] 支持2021.3.28和2022.3.4版本
- [opt] 删除MachineState::InitEvalStack分配_StackBase后不必要的memset
- [fix] 修复Exception机制的bug
- [fix] 修复CustomAttribute不支持Type[]类型参数的bug
- [fix] 修复不支持new string(xxx)用法的问题
- [refactor] 重构VTableSetup实现
- [fix] 修复未计算子interface中显式实现父interface的函数的bug
- [opt] Lazy初始化CustomAttributeData，而不是加载时全部初始化，明显减少Assembly.Load时间
- [fix] 修复2022 当new byte\[]{a,b,c...}方式初始化较长的byte[]数据时，返回错误数据的bug

### Editor

- [fix] 修复计算桥接函数未考虑到泛型类的成员函数中可能包含的Native2Managed调用
- [change] link.xml及AOTGenericReferences.cs默认输出路径改为HybridCLRGenerate，避免与顶层HybridCLRData混淆
- [fix] 修复Win下生成的Lump文件中include路径以\为目录分隔符导致同步到Mac后找不到路径的bug
- [refactor] 重构Installer


## 3.3.0 

发布日期 2023.07.03。

### Runtime

- [fix] 修复localloc分配的内存未释放的bug
- [change] MachineState改用RegisterRoot的方式注册执行栈，避免GC时扫描整个堆栈
- [opt] 优化Managed2NativeCallByReflectionInvoke性能，提前计算好传参方式
- [refactor] 重构ConvertInvokeArgs

### Editor

- [fix] 修复2020-2021编译libil2cpp.a未包含brotli相关代码文件导致出现编译错误的bug
- [fix] 修复从导出xcode项目包含绝对路径导致传送到其他机器上编译时找不到路径的bug
- [fix] 解决Generate LinkXml、 MethodBridge、AOTGenericReference、ReversePInvokeWrap 生成不稳定的问题
- [fix] 修复使用不兼容版本打开Installer时出现异常的bug
- [change] 禁用hybridclr后打包ios时不再修改导出的xcode工程

## 3.2.1

### Runtime

- [fix] 修复il2cpp TypeNameParser未将类型名中转义字符'\'去掉，导致找不到嵌套子类型的bug

### Editor

- [new] Installer界面新增显示package版本
- [new] CompileDll新增MacOS、Linux、WebGL目标
- [fix] 修复重构文档站后的帮助文档的链接错误
- [change] 为Anaylizer加上using 限定，解决某些情况下与项目的类型同名而产生编译冲突的问题

## 3.2.0

### Runtime

- [fix] 修复未在PlaceHolder中的Assembly加载时，如果由于不在Assembly列表，也没有任何解释器栈，导致Class::resolve_parse_info_internal查找不到类型的bug
- [fix] 修复读取CustomAttribute System.Type类型数据崩溃的bug

### Editor

- [new] 支持直接从源码打包iOS，不再需要单独编译libil2cpp.a
- [opt] 优化版本不兼容时错误提示，不再抛出异常，而是显示"与当前版本不兼容"


## 3.1.1

### Runtime

- [fix] 修复2021及更高版本，InterpreterModule::Managed2NativeCallByReflectionInvoke调用值类型成员函数时，对this指针多余this=this-1操作。
- [fix] 修复解析CustomAttribute中Enum[]类型字段的bug
- [fix] 修复2021及更高版本反射调用值类型 close Delegate的Invoke函数时未修复target指针的bug
- [new] 新增对增量式GC宏的检查，避免build.gradle中意外开启增量式GC引发的极其隐蔽的问题

### Editor

- [fix] 修复 Win32、Android32、WebGL平台的编译错误
- [fix] 修复计算桥接函数时未考虑到补充元数据泛型实例化会导致访问到一些非公开的函数的情况，导致少生成一些必要的桥接函数
- [opt] 生成AOTGenericReferences时，补充元数据assembly列表由注释改成List<string>列表，方便在代码中直接使用。
- [change] CheckSettings中不再自动设置Api Compatible Level

## 3.1.0

### Runtime

- [rollback] 还原对Unity 2020.3.x支持
- [fix] 修复 WebGL平台ABI的bug

### Editor

- [rollback] 还原对Unity 2020.3.x支持

## 3.0.3

### Runtime

- [fix] 修复Enum::GetValues返回值不正确的bug

## 3.0.2

### Runtime

- [fix] 修复Memory Profiler中创建内存快照时崩溃的bug

### Editor

- [remove] 移除 `HybridCLR/CreateAOTDllSnapshot`菜单


## 3.0.1

### Runtime

- [new] 支持2022.3.0

## 3.0.0

### Runtime

- [fix] 修复不支持访问CustomData字段及值的bug
- [remove] 移除对2019及2020版本支持

### Editor

- 包名更改为com.code-philosophy.hybridclr
- 移除UnityFS插件
- 移除Zip插件
- HybridCLR菜单位置调整

## 2.4.2

### Runtime

- [version] 支持 2020.3.48，最后一个2020LTS版本
- [version] 支持 2021.3.25

## 2.4.1

### Runtime

### Editor

- [fix] 修复遗漏 RELEASELOG.md.meta 文件的问题

## 2.4.0

### Runtime

### Editor

- [new] CheckSettings中检查ScriptingBackend及ApiCompatibleLevel，切换为正确的值
- [new] 新增 MsvcStdextWorkaround.cs 解决2020 vs下stdext编译错误的问题
- [fix] 修复当struct只包含一个float或double字段时，在arm64上计算桥接函数签名错误的bug

## 2.3.1

### Runtime

### Editor

- [fix] 修复本地复制libil2cpp却仍然从仓库下载安装的bug

## 2.3.0

### Runtime

### Editor

- [new] Installer支持从本地目录复制改造后的libil2cpp
- [fix] 修复2019版本MonoBleedingEdge的子目录中包含了过长路径的文件导致Installer复制文件出错的问题


