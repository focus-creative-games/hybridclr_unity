---
name: hybridclr-hotupdate-assembly
description: >-
  Split, create, compile, and load HybridCLR hot-update assemblies: asmdefs and Assembly-CSharp, forbidding AOT references to hot-update code, CompileDll, Assembly.Load order, and calling hot-update entry points from the main project. Use when the user adds a hot-update module, splits AOT and hot-update code, compiles HotUpdate.dll, loads a dll, hits a duplicate load, or a player build references a hot-update assembly.
---

# Hot-update assemblies

Docs: `D:/workspace/hybridclr-doc/docs/guide/setup/hotupdateassemblysetting.md`, `guide/build/compileassembly.md`, and `guide/build/runhotupdatecodes.md`. Settings fields are in `hybridclr-configure`.

## Split

Keep at least one **AOT** assembly for startup and for downloading hot-update content. Hot-update code must live in its own assembly.

Two common layouts:

- `Assembly-CSharp` stays AOT, and other code is split into AOT and hot-update asmdefs. **Turn off Auto Referenced on hot-update asmdefs.** `Assembly-CSharp` references every other assembly, so Auto Referenced easily creates an AOT reference to a hot-update dll and the player build fails.
- `Assembly-CSharp` itself is hot-update. Put the name in `hotUpdateAssemblies` without `.dll`.

AOT assemblies must not reference hot-update assemblies. Hot-update assemblies may reference each other. Load dependencies first.

## Create

1. Create a folder, for example `Assets/HotUpdate`.
2. Create → Assembly Definition, named like the module.
3. In `HybridCLR/Settings`, put asmdefs in `hotUpdateAssemblyDefinitions`. Put assemblies that have no asmdef in `hotUpdateAssemblies` by name. Do not add the same assembly to both.
4. For a prebuilt dll outside the default output directory, put the name in `hotUpdateAssemblies` and the directory in `externalHotUpdateAssembliyDirs` (relative to the project root).

## Compile

Do not ship dlls from `Library/ScriptAssemblies`. Those are compiled for the Editor target and depend on the debugger setting.

- Use `HybridCLR/Compile/{platform}`, or `HybridCLR.Editor.Commands.CompileDllCommand.CompileDll(BuildTarget)`.
- Output: `{project}/HybridCLRData/HotUpdateDlls/{target}`. Compile once per BuildTarget so defines such as `UNITY_ANDROID` match.
- `HybridCLR/Generate/All` also compiles hot-update dlls. **After the player is shipped**, a normal hot update only recompiles and publishes the dll. Do not run Generate every time.

When copying into assets, add a `.bytes` extension (`HotUpdate.dll.bytes`).

## Load

```csharp
// The runtime copies the bytes. Do not keep assemblyData or pdb after the call returns.
Assembly ass = Assembly.Load(assemblyData);
// Since v6.4.0, symbols can load with the assembly:
Assembly ass2 = Assembly.Load(assemblyData, pdbSymbolData);
```

- `Assembly.Load` multiple dlls in dependency order. Move the call to another thread if it stalls the main thread.
- **Do not Load again in the Editor.** The assemblies are already in the domain. Wrap the load in `#if !UNITY_EDITOR` and, in the Editor, find them with `AppDomain.CurrentDomain.GetAssemblies()`.
- An assembly missing from the hot-update lists is compiled into AOT. Loading it again at runtime is a duplicate load.

## Call hot-update code from AOT

The main project cannot reference hot-update types directly. Typical entry points:

- Reflection: `ass.GetType(...).GetMethod(...).Invoke`
- `Delegate.CreateDelegate`, or `Activator.CreateInstance` cast to an interface defined in AOT
- `go.AddComponent(type)` after the dll is loaded
- Instantiate a prefab or scene packed in an AssetBundle (extra rules for scripts on assets are in `hybridclr-monobehaviour`)

Load the dll **before** loading an AssetBundle that should restore those scripts.
