# 发布日志

## 2.4.2

### 改动

- 支持 2020.3.48，最后一个2020LTS版本
- 支持 2021.3.25
- hybridclr 升级到 v2.3.1
- il2cpp_plus 升级到 v2020-2.2.1 、v2021-2.2.1

## 2.4.1

### 修复

- 修复遗漏 RELEASELOG.md.meta 文件的问题

## 2.4.0

### 改动

- CheckSettings中检查ScriptingBackend及ApiCompatibleLevel，切换为正确的值
- hybridclr 升级到v2.3.0版本
- il2cpp_plus 升级到v2019-2.2.0、v2020-2.2.0、v2021-2.2.0版本

### 修复

- 新增 MsvcStdextWorkaround.cs 解决2020 vs下stdext编译错误的问题
- 修复当struct只包含一个float或double字段时，在arm64上计算桥接函数签名错误的bug

## 2.3.1

### 修复
-  修复本地复制libil2cpp却仍然从仓库下载安装的bug

## 2.3.0

### 改动

- Installer支持从本地目录复制改造后的libil2cpp

### 修复

- 修复2019版本MonoBleedingEdge的子目录中包含了过长路径的文件导致Installer复制文件出错的问题



