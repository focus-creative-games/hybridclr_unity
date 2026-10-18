---
name: hybridclr-configure
description: >-
  Configure HybridCLR Settings and PlayerSettings: enable, hot-update assembly lists, external dll search paths, link.xml output, linkPreserveLevel, incremental GC, and IL2CPP. Use when the user opens HybridCLR/Settings, edits ProjectSettings/HybridCLRSettings.asset, checks settings before a player build, or asks what a setting does.
---

# Configure HybridCLR

Docs: `D:/workspace/hybridclr-doc/docs/guide/setup/projectsettings.md` and `hybridclr-unity-package.md`. Fields follow `Editor/Settings/HybridCLRSettings.cs` in this repo. The asset is `ProjectSettings/HybridCLRSettings.asset`.

How to split assemblies and create asmdefs is `hybridclr-hotupdate-assembly`. Do not expand that here.

## PlayerSettings

- Scripting Backend is **IL2CPP**. WebGL does not use this option. Since v2.4.0 the build check switches this automatically.
- Api Compatibility Level is **.NET Framework**.
- Incremental GC is allowed since v4.0.0. Older packages must turn it off.

## Hot-update lists

`hotUpdateAssemblyDefinitions` and `hotUpdateAssemblies` merge into the final hot-update dll list. **Do not put the same assembly in both.**

| Source | Where |
|---|---|
| asmdef | `hotUpdateAssemblyDefinitions` |
| External dll, or `Assembly-CSharp` | `hotUpdateAssemblies`, name **without** `.dll` |

An empty list fails the player-build check. `IFilterBuildAssemblies` removes these dlls from the player build so they are not compiled into AOT.

## Settings

- `enable`: default true. Turning it off drops HybridCLR from the player. Also remove the main project's reference to `HybridCLR.Runtime`, or symbols such as `LoadMetadataForAOTAssembly` are missing.
- `useGlobalIl2cpp`: default false. Keep the local `UNITY_IL2CPP_PATH`.
- `hybridclrRepoURL` / `il2cppPlusRepoURL`: clone URLs for the installer. Default is gitee.
- `preserveHotUpdateAssemblies`: names of hot-update dlls that do not exist yet (no `.dll`). The build does not check that the files exist, but it writes the names into `ScriptingAssemblies.json` so scripts can be attached to assets later. Do not list dlls that already exist.
- `externalHotUpdateAssembliyDirs`: search paths for external hot-update dlls. The serialized name is spelled `Assembliy`. Do not rename it. Paths are relative to the **project root** (parent of Assets). Each entry is searched as `{dir}/{platform}` first, then `{dir}`. Earlier entries win. The dll name still goes in `hotUpdateAssemblies`.
- `outputLinkFile`: output of `Generate/LinkXml`. Default `HybridCLR/Generate/link.xml`, relative to Assets. **Do not** point it at `Assets/link.xml`. Generation overwrites the file.
- `linkPreserveLevel`: `PreserveAllOfDeclaringType` (default; keep every member of a referenced type) or `PreserveExactMember` (only referenced fields, methods, properties, and events; smaller).
- `outputAOTGenericReferenceFile`: default `HybridCLR/Generate/AOTGenericReferences.cs`.
- `maxGenericReferenceIteration` / `maxMethodBridgeGenericIteration`: generic scan iteration cap, default 10. Raise them only if the log shows the scan has not converged.

## Fixed output directories

These are not settings:

- Hot-update dlls: `{project}/HybridCLRData/HotUpdateDlls/{target}`
- Stripped AOT dlls: `{project}/HybridCLRData/AssembliesPostIl2CppStrip/{target}`

## What the package does during a player build

Do not hand-edit these: set or clear the process `UNITY_IL2CPP_PATH` from `enable`; strip hot-update assemblies from the build; write hot-update dll names into the assembly list; copy stripped AOT dlls to the directory above.
