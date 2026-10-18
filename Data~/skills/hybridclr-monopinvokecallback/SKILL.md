---
name: hybridclr-monopinvokecallback
description: >-
  Reserve MonoPInvokeCallback and ReversePInvokeWrapper slots for HybridCLR hot-update methods: ReversePInvokeWrapperGenerationAttribute, Generate/ReversePInvokeWrapper, and the lack of automatic marshaling for string and other non-blittable arguments. Use for MonoPInvokeCallback, xlua/tolua/puerts callbacks, too few wrappers, or a crash when native code calls hot-update C#.
---

# MonoPInvokeCallback

Doc: `guide/features/monopinvokecallback.md` in the HybridCLR documentation. Choosing a Lua, JS, or Python stack is `hybridclr-script-language`.

A static method with `[MonoPInvokeCallback]` needs a C++ wrapper before `Marshal.GetFunctionPointerForDelegate` can return a pointer. Platforms that cannot JIT, including iOS, cannot create that wrapper at runtime. Generate it in the player build with `HybridCLR/Generate/ReversePInvokeWrapper` (`Generate/All` includes it).

## Reserve a count

Generating one wrapper per current callback runs out as soon as a later hot update adds another callback.

Add `[ReversePInvokeWrapperGeneration(preserveCount)]` (`HybridCLR` namespace) on the callback. **Counts are per signature:**

wrappers for a signature = sum of `preserveCount` on that signature + number of callbacks of that signature that do not have the attribute.

```csharp
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
delegate int LuaFunction(IntPtr luaState);

[ReversePInvokeWrapperGeneration(10)]
[MonoPInvokeCallback(typeof(LuaFunction))]
public static int LuaCallback(IntPtr luaState) => 0;
```

The reservation is fixed in the player. Hot updates can only consume wrappers that were already generated.

## Argument marshaling

HybridCLR does **not** marshal arguments when calling these callbacks. `int` and `float` work. `string` arrives as `char*` and using it as a string crashes.

Two options:

1. Put the callback in AOT and call into hot-update code from there. The AOT wrapper has il2cpp marshaling.
2. Take `IntPtr` in the hot-update callback and convert with `Marshal.PtrToStringUTF8`. Do the same for other non-primitive types that need marshaling.
