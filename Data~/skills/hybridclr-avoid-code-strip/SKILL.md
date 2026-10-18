---
name: hybridclr-avoid-code-strip
description: >-
  Keep Unity IL2CPP from stripping AOT types and methods that hot-update code calls: Generate/LinkXml, linkPreserveLevel, a hand-written Assets/link.xml, a real reference from the main project, and MissingMetadataChecker. Use for TypeLoadException, MissingMethodException that is not an AOT generic error, code stripping, link.xml, or Generate/LinkXml.
---

# Avoid code stripping

Doc: `D:/workspace/hybridclr-doc/docs/guide/build/codestriping.md`.

If a stripped AOT type or method is called from hot-update code:

```text
TypeLoadException: Could not load type 'Xxx' from assembly 'yyy'
MissingMethodException: ...
```

If the log says `AOT generic method not instantiated`, use `hybridclr-aot-generic`. link.xml does not fix that.

## Before the player build

Run `HybridCLR/Generate/All` before building the player. It includes `Generate/LinkXml`. LinkXml alone scans AOT references from the current hot-update dlls.

- Output is Settings `outputLinkFile`, default `Assets/HybridCLR/Generate/link.xml`.
- **Do not** point `outputLinkFile` at a hand-maintained `Assets/link.xml`. Generation overwrites it.
- `linkPreserveLevel`: default `PreserveAllOfDeclaringType` keeps the whole referenced type. Use `PreserveExactMember` only to shrink the player (referenced fields, methods, properties, and events). A later hot update that touches another member can then miss it.

## When link.xml has no effect

If the main project never references any type or method in an AOT assembly, Unity strips that assembly entirely and preserve entries are ignored. Reference one type or method from that dll in the main project.

`preserve all` on `UnityEngine.Debug` can hit a Unity bug. Since 6.1.0, generated link.xml skips that class. Do not add it back.

## Reserve types for later hot updates

`Generate/LinkXml` only covers references in the **current** hot-update dlls. Put types you will need later in a separate `Assets/link.xml`, or call them from the main project. Do not put them in the generated file.

## Check before publishing a hot update

Use the stripped AOT dll directory saved from the player build. Do not use `AssembliesPostIl2CppStrip` after a later `Generate/All` has refreshed it.

```csharp
var checker = new HybridCLR.HotUpdate.MissingMetadataChecker(
    aotDirFromPlayerBuild, SettingsUtil.HotUpdateAssemblyNamesIncludePreserved);
bool ok = checker.Check(hotUpdateDllPath); // false: the dll touches a stripped type or method
```

The second argument is hot-update assembly names without `.dll`. If an Ultimate build also has DHE assemblies, include those names in the list.
