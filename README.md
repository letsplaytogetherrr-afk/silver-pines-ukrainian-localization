# Silver Pines Ukrainian Localization — plugin source

Author: **Skarig**. Plugin ID: `skarig.silverpines.localization`. Plugin version: **0.1.9**.

This is the C# source for the localization plugin included in `Silver_Pines_Ukrainian_Mod.zip`. It is provided for source inspection and rebuilding, including manual file review by Nexus Mods. The source file is an unchanged copy of the source used for that release.

## What the plugin does

- Adds Ukrainian as a separate text locale; the original locales remain available.
- Registers an additional Addressables catalog for 24 Ukrainian string tables.
- Redirects selected bundle-loading requests to separate translated copies, without overwriting original game files.
- Adds a main-menu texture selector. Texture selection takes effect after a full game restart.
- Uses extended copies of the existing Cyrillic fonts while preserving the original family/weight mapping.
- Applies localized phone/vending display messages and hides the requested hover-text component when Ukrainian textures are selected.
- Preserves the original game's raw-string fallback for optional/empty localization references.

There is **no voiceover plugin, voice selector, audio bank, downloader, telemetry client, or executable installer in this source repository**. The `SkarigVoiceLanguage` string in the source is only a compatibility check for a font on an optional separately installed plugin's menu row; it does not create that row.

## Build and review

- [Build instructions](BUILD.md)
- [Architecture and review notes](docs/ARCHITECTURE.md)
- [Third-party components](THIRD_PARTY.md)
- [Public archive contents and file review](docs/FILE_REVIEW.md)
- [Release hashes](release-info.json)

The supported baseline is Silver Pines for Windows x64, Steam app **2333000**, build **26489514**, Unity **6000.3.15f1**, with BepInEx **6.0.0-be.791+b782da4**. A different game build can change the interop API and generated coroutine names; a successful compile alone is not a runtime compatibility test.

## What is not in this repository

No game assemblies, game metadata, interop assemblies generated from the game, textures, translation workbook, translated bundles, audio, personal configuration, API keys, or compiled binaries are included. Obtain the game through its legitimate distribution and the loader/compiler from their official sources. The separate localization data is distributed with the mod download, not this source repository.

The build script rebuilds **the plugin DLL**. It does not recreate the artwork or the data bundles, and it does not install anything. To run a rebuilt DLL, use the matching data from the same mod release. Do not put compiler tools or generated game interop assemblies in this repository.

## Installation of the released mod

With the game closed, extract the public mod archive beside `Carpenter.exe`. It adds `BepInEx`, `dotnet`, `winhttp.dll`, `doorstop_config.ini`, and `.doorstop_version`. It does not replace `Carpenter_Data` contents. The first BepInEx launch generates interop support files and can take longer than subsequent launches. The voice selector is absent because the separate voice plugin is not included.

For a clean test, move any old mod installation to a backup first. Steam uninstall can leave third-party mod files behind. This package does not automatically delete existing mods.

Source availability here does not grant rights to third-party game assets. Third-party components retain their own licenses; see THIRD_PARTY.md. No game or third-party library source has been copied into this repository.
