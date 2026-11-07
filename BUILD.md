# Building the plugin on Windows

## Prerequisites

1. A locally installed copy of **Silver Pines**, baseline Steam build **26489514**, Windows x64.
2. **BepInEx Unity IL2CPP for Windows x64, 6.0.0-be.791+b782da4**, from the official [BepInEx build archive](https://builds.bepinex.dev/projects/bepinex_be). Select build **791**, `Unity.IL2CPP-win-x64`; do not use the Mono package. [Official IL2CPP installation guide](https://docs.bepinex.dev/master/articles/user_guide/installation/unity_il2cpp.html).
3. The BepInEx runtime folders `BepInEx/core` and `dotnet` from that distribution. These are not committed to this repository.
4. Generated interop assemblies in `<game>/BepInEx/interop`. Start the game once with the loader installed, wait for initialization, then close it. The BepInEx setting `UpdateInteropAssemblies` must be enabled. Interop assemblies are generated from the locally installed game; do not upload them to GitHub.
5. **Microsoft.Net.Compilers.Toolset 4.8.0**, available from the official [NuGet package page](https://www.nuget.org/packages/Microsoft.Net.Compilers.Toolset/4.8.0). Download the `.nupkg`, extract it as a ZIP outside the repository, and locate `tasks/net472/csc.exe`. The release used this compiler version. Its Windows compiler requires the corresponding .NET Framework runtime; .NET Framework 4.8 is suitable.
6. Windows PowerShell 5.1 or PowerShell 7.

This deliberately uses a direct Roslyn compilation with `/nostdlib+`, matching the original build approach. The matching BepInEx .NET runtime provides the managed system references; generated game interop provides Unity and Carpenter API references. No proprietary references are redistributed here.

## Build command

From the repository folder, run:

```powershell
.\build.ps1 `
  -GameDirectory 'C:\Games\Silver Pines' `
  -CompilerPath 'C:\Tools\Microsoft.Net.Compilers.Toolset.4.8.0\tasks\net472\csc.exe'
```

Replace those two example paths with your local locations. If your PowerShell execution policy prevents running the script, inspect the script and apply your normal local script execution policy; the script does not change that policy.

Output:

```text
artifacts/SilverPinesLocalization.dll
artifacts/build.rsp
```

The script prints the rebuilt DLL's SHA256. `artifacts` and response files are ignored by Git because generated files contain local reference paths. No game files are modified and the game is not launched by the script.

If the loader distribution is in another directory, use the optional parameter:

```powershell
.\build.ps1 `
  -GameDirectory 'C:\Games\Silver Pines' `
  -LoaderDirectory 'C:\Tools\BepInEx-Unity.IL2CPP-win-x64' `
  -CompilerPath 'C:\Tools\Microsoft.Net.Compilers.Toolset.4.8.0\tasks\net472\csc.exe'
```

The game directory supplies `BepInEx/interop`; LoaderDirectory supplies `BepInEx/core` and `dotnet`. The script excludes native DLLs from compiler references by checking their managed assembly identity.

## Rebuild versus exact binary reproduction

The released source and released DLL/archive SHA256 values are recorded in `release-info.json`. This rebuild uses the same C# source and compiler family, but **byte-for-byte identical output is not promised**: the original release was not built with deterministic-build options, and generated interop references can vary. For review, compare source, managed code, and behavior rather than assuming every rebuild must have the release DLL's hash.

## Running a rebuilt plugin

Use a backup/test installation. Close the game, place the rebuilt DLL in `BepInEx/plugins/SilverPinesLocalization`, and keep the matching release's `texts.json`, `bundles.json`, `uk_asset_paths.json`, `UkrainianTexts.bundle`, and referenced `Bundles` files beside it. Building the DLL alone does not produce that data.

Check normal startup, Continue/load, Ukrainian/original language selection, fonts after scene changes, texture selection plus restart, opening/closing an examined sign, and the phone's coin/number/calling states. Inspect `BepInEx/LogOutput.log` for failed preflight or hook errors. Do not treat a successful compile as proof that the game loaded successfully.
