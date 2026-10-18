---
name: hybridclr-setup
description: >-
  Install and initialize com.code-philosophy.hybridclr: pick a matching Unity and package version, add the package from git or a local Packages folder, run HybridCLR/Installer, and recover from git or network failures. Use when the user installs, upgrades, or initializes HybridCLR, or when install fails, git clone fails, or the player still uses stock IL2CPP.
---

# Install HybridCLR

Docs: `guide/setup/install.md` and `guide/setup/supported-platforms-and-unity-versions.md` in the HybridCLR documentation. Package and il2cpp branches come from this repo's `Data~/hybridclr_version.json`. Do not change those branches from memory.

## Versions

- **v9.x** supports Unity **2022.3.x** and **6000.x** except 6000.5.x. Use **v8.x** for 2019.4, 2020.3, and 2021.3.
- The package name is `com.code-philosophy.hybridclr`. Before v3.0.0 it was `com.focus-creative-games.hybridclr_unity`.
- Standalone players need the **IL2CPP** module (`Windows Build Support (IL2CPP)` or `Mac Build Support (IL2CPP)`). An editor that only has the iOS or Android module cannot build that Standalone player. Exporting an iOS Xcode project does not require a local signing certificate.

## Install the package

Either:

1. Package Manager → Add package from git URL: `https://gitee.com/focus-creative-games/hybridclr_unity.git` or the GitHub repo with the same name. Pin a tag with `#{tag}`, for example `#v9.0.0`.
2. Place the repo at `Packages/com.code-philosophy.hybridclr`.

## Initialize (required)

Installing the package does not replace libil2cpp.

1. **git** must be installed. Restart the Unity Editor and Hub after installing git. Restart the machine if git is on PATH but the installer still cannot find it.
2. Open `HybridCLR/Installer...` and use the default remote install. Success prints `安装成功` and the status becomes installed.
3. The installer clones the `hybridclr` and `il2cpp_plus` branches listed in `Data~/hybridclr_version.json` for the current Unity major version. Do not edit that file unless the user explicitly wants a non-matching version.
4. If git or the network is unavailable, merge a local libil2cpp that already contains HybridCLR, enable "copy libil2cpp from local" in the installer, and install. Merge `il2cpp_plus/libil2cpp` with `hybridclr/hybridclr` into the final libil2cpp (see the install doc).

Local install lands in `{project}/HybridCLRData/LocalIl2CppData-{platform}/`. The package sets `UNITY_IL2CPP_PATH` only for the **current Editor process**. Leave `useGlobalIl2cpp` off.

## Updates

After updating the package, run `HybridCLR/Installer` again. The installer clears `Library/Il2cppBuildCache`. If libil2cpp was replaced by hand, delete that cache or the player build keeps the old IL2CPP.

## Host tools

- Windows: Visual Studio 2022 or newer with the Unity game development and C++ game development workloads, plus git.
- Mac: macOS 12 or newer, Xcode 13 or newer, plus git and cmake.

## Do not

- Do not replace libil2cpp inside the Editor install unless the user explicitly wants a global install and accepts that other projects are affected and that every `Generate` must be copied back over the Editor libil2cpp.
- WebGL uses the same local install as other platforms.
