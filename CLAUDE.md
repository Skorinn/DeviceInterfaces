# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A single-assembly **.NET Framework 4.8 class library** (`DeviceInterfaces.dll`) that wraps Win32 device-management APIs for consumption by other Windows desktop apps. It has no entry point, no tests, and no external package references — only GAC references to `System.*`.

Today the library contains one type: `DeviceInterfaces.USBDeviceNotification`, a static P/Invoke wrapper over `user32!RegisterDeviceNotification` / `UnregisterDeviceNotification`.

## Build

This is a **legacy (non-SDK) csproj**, so build with full MSBuild — not the `dotnet` CLI:

```
"C:\Program Files\Microsoft Visual Studio\18\Enterprise\MSBuild\Current\Bin\MSBuild.exe" DeviceInterfaces.sln -p:Configuration=Debug
```

Locate MSBuild portably with:
```
"C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe" -latest -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe"
```

Output lands in `bin\Debug\` (or `bin\Release\`), and a post-build `xcopy` step also drops the DLL in `bin\` at the solution root — that flat `bin\` copy is the artifact consuming solutions are expected to reference.

**New source files must be added manually** to the `<Compile Include="..." />` ItemGroup in `DeviceInterfaces.csproj`; the legacy project format does not glob.

## Consumer contract

`USBDeviceNotification` only performs registration. The *host application* owns the message loop, so using it correctly is a two-sided arrangement a caller must implement:

1. Call `RegisterUsbDeviceNotification(hwnd)` with a window handle (e.g. WinForms `this.Handle`) and keep the returned `IntPtr`.
2. Override the window procedure and watch for `iWM_DEVICECHANGE` (0x0219), comparing `wParam` against the exposed `iDEVICE_CONNECTED` (0x8000) and `iDEVICE_REMOVED` (0x8004) constants.
3. Call `UnregisterUsbDeviceNotification(handle)` with the stored handle on teardown.

The registration is hard-coded to `DBT_DEVTYP_DEVICEINTERFACE` (5) filtered on the USB device interface class GUID `A5DCBF10-6530-11D2-901F-00C04FB951ED`. Filtering for a different device class means a new API surface, not a change to this one.

`RegisterUsbDeviceNotification` throws `Win32Exception` when registration fails rather than returning `IntPtr.Zero`, so callers registering from a window-handle-created hook should expect that. `UnregisterUsbDeviceNotification` is deliberately quiet: it ignores `IntPtr.Zero` and swallows failures, since it runs on teardown paths.

## House style

`USBDeviceNotification.cs` is the reference for conventions in this codebase; match it:

- Every file opens with the boxed header comment block: file name, description, `Copyright (C) <year> Mike Pullen` followed by the MIT license pointer line, then a dated `Revision History` line per change. Copy the header from `USBDeviceNotification.cs` verbatim — the project is MIT licensed, so new files must not reintroduce the old "All Rights Reserved / Confidential and Proprietary" wording.
- Members are grouped in `#region` blocks in this order: `Externals`, `Type definitions`, `Methods`, `Data Members` — with data members **last**, not first.
- Hungarian-style prefixes: `i` for ints, `p` for `IntPtr`/pointers, `m_` for private fields and private constants. Public constants keep the type prefix but drop `m_` (`iDEVICE_CONNECTED`).
- XML doc comments on every public member; parameters documented as `IN - description`.

## Releasing

`Properties/AssemblyInfo.cs` is the single source of truth for the release version — the release
workflow reads `AssemblyVersion` from it rather than taking a version input, and fails if
`AssemblyVersion` and `AssemblyFileVersion` disagree or the version was already released. Bump both
together as `X.Y.Z.0` when preparing a release.

Releases run only from `master` and pause for approval on the `release` GitHub environment before
anything is tagged or published. See `docs/RELEASING.md`.
