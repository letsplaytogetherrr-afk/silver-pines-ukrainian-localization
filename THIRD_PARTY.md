# Third-party components and provenance

This source repository contains only the localization plugin source, build script, and review documentation. Dependency binaries are obtained separately and are not vendored here.

| Component | Release baseline / purpose | Official source |
| --- | --- | --- |
| BepInEx Unity IL2CPP | 6.0.0-be.791+b782da4, Windows x64; loads the plugin | https://github.com/BepInEx/BepInEx and https://builds.bepinex.dev/projects/bepinex_be |
| Unity Doorstop | 4.6.0 as bundled with that BepInEx distribution; native loader entry point | https://github.com/NeighTools/UnityDoorstop |
| Il2CppInterop and Harmony support | As bundled with the specified BepInEx build; generated interop and runtime hooks | https://github.com/BepInEx/Il2CppInterop |
| HarmonyX / loader-provided patching libraries | 0Harmony.dll identifies itself as HarmonyX 2.10.2 in this loader | https://github.com/BepInEx/HarmonyX and the BepInEx distribution's component notices |
| .NET runtime | Loader-provided runtime; the tested installation reports .NET 6.0.7 | https://github.com/dotnet/runtime |
| Roslyn compiler | Microsoft.Net.Compilers.Toolset 4.8.0; build-time only, not part of the mod archive | https://www.nuget.org/packages/Microsoft.Net.Compilers.Toolset/4.8.0 and https://github.com/dotnet/roslyn |
| Unity managed base library ZIP | `6000.3.15.zip`; BepInEx's first-run interop input/cache | https://unity.bepinex.dev/libraries/6000.3.15.zip |
| Silver Pines game and generated game interop | Required locally for building; not redistributed in this repository | The user's legitimate Steam installation, app 2333000 |

The public download includes the loader's necessary runtime dependencies in addition to the custom plugin and localized data. This table identifies the main dependency groups; the loader distribution contains additional support libraries. Refer to the upstream distribution for the complete component list and applicable notices/licenses. Listing a component does not relicense it or make it part of this repository's authored code.

No original game executable, `GameAssembly.dll`, `global-metadata.dat`, or generated game interop DLL is committed here. No voice-bank modification tool is required to build this plugin.
