---
name: hybridclr-aot-generic
description: >-
  Handle HybridCLR AOT generics: supplementary metadata via LoadMetadataForAOTAssembly, saving stripped AOT dlls per platform, HomologousImageMode, AOTGenericReference, and MissingMethodException for an AOT generic method. Use when an AOT generic fails to instantiate, the user asks about metadata patching, List/Dictionary/async generics, or which supplementary dll to ship.
---

# AOT generics

Docs: `guide/build/aotgeneric.md` and `start/generic.md` in the HybridCLR documentation.

Generic types and methods **defined in hot-update assemblies** have no AOT restriction. The restriction is hot-update code using a generic **defined in an AOT assembly** (`List<T>`, `Dictionary<TKey,TValue>`, async state machines, and similar) when that instantiation was not generated in AOT. Typical log:

```text
MissingMethodException: AOT generic method not instantiated in aot module
```

## Default: supplementary metadata

The community edition uses supplementary metadata. Do not send the user to hand-written RefTypes unless they are chasing one instantiation or optimizing a hot path.

Patch the **AOT dll that contains the generic method**, not the dll that contains the generic argument. `List<SomeHotUpdateStruct>` needs `mscorlib.dll`, because the missing IL is `List<T>.Add`, not the struct.

```csharp
// Call once before any hot-update code uses that AOT generic. Order among dlls does not matter.
LoadImageErrorCode err = HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly(dllBytes, HomologousImageMode.SuperSet);
// Do not keep dllBytes. The API copies them.
```

- Call it after player startup, or after hot-update dlls are loaded and before business code runs. The supplementary dlls can ship in the player (for example StreamingAssets) or with a hot update.
- Call it on another thread if it stalls.
- Loaded metadata uses several times the dll size and is not released. Memory-tight projects use the commercial full generic sharing build (no supplementary dlls), or run `AOTAssemblyMetadataStripper.Strip` to drop non-generic method metadata first.
- `HybridCLR/Generate/AOTGenericReference` writes a conservative `PatchedAOTAssemblyList`. Load the whole list when memory is fine. Otherwise start with common dlls such as `mscorlib` and add a dll when a new AOT generic error appears. Supplementary dlls can themselves be hot-updated.

## Which dll

The player build copies stripped AOT dlls to `{project}/HybridCLRData/AssembliesPostIl2CppStrip/{target}`. `Generate/AotDlls` can also produce them by exporting a temp project.

- **Stripped dlls from different BuildTargets are not interchangeable.**
- **Freeze the dlls from the shipped player.** Do not replace them with a newer Generate output on every hot update.
- `HomologousImageMode.SuperSet`: a superset is enough. Stripped or original AOT dlls both work. Use this by default.
- `HomologousImageMode.Consistent`: the bytes must match **that player build's** stripped dll. Original dlls do not work. `Generate/All` output often differs slightly from the real player, so Consistent fails more often.

## Generic sharing (optional)

After metadata is loaded, HybridCLR still prefers an il2cpp shared instantiation when one exists. Class type arguments share as `object`. Non-enum value types share only with themselves. Enums share with enums of the same underlying type. Value types do not share across different types the way classes do, so `List<HotUpdateStruct>` is not covered by an AOT `List<int>`.

Instantiations in `AOTGenericReferences.cs` are comments and are overwritten on regenerate. Copy any you want into another AOT file (for example `new List<object>()`). That is a performance optimization, not a requirement for correctness.

## Full generic sharing

Commercial editions only. It removes the size, memory, and interpreted-call cost of supplementary metadata. Do not switch the workflow to it unless the user has that edition.
