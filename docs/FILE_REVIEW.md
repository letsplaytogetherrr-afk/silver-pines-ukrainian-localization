# Public download review notes

Download under review: **Silver_Pines_Ukrainian_Mod.zip**. See `release-info.json` for its size and SHA256 and the exact released custom DLL/source hashes.

## Expected top-level contents

```text
winhttp.dll
doorstop_config.ini
.doorstop_version
dotnet/
BepInEx/core/
BepInEx/config/BepInEx.cfg
BepInEx/config/skarig.silverpines.localization.cfg
BepInEx/unity-libs/6000.3.15.zip
BepInEx/plugins/SilverPinesLocalization/
```

The only DLL under `BepInEx/plugins` in that release is **SilverPinesLocalization.dll**, whose complete authored C# source is `src/LocalizationPlugin.cs` in this repository. `BepInEx/core` and `dotnet` are third-party loader/runtime components, not additional custom plugins. There is no executable installer.

The nested `BepInEx/unity-libs/6000.3.15.zip` is the cached Unity managed base libraries consumed by BepInEx's interop generator. It is not a second mod or a hidden executable installer. The native loader `winhttp.dll` is the Doorstop component distributed with BepInEx. Official dependency sources are listed in THIRD_PARTY.md.

Plugin-local `.bundle` files and the three JSON data files contain the localized resource copies, table paths, and resource manifest. Their presence is necessary for running the plugin; they are not included in this source repository. The release contains no `Carpenter_Data` replacements, game executable, original game metadata, audio/voice banks, voice selector plugin, personal API keys, logs, compiler, or generated interop directory.

The release packaging audit checked every ZIP member's extracted SHA256 against its staged file and checked that no voice plugin/banks or unlisted files were included. This integrity audit is **not** an antivirus verdict and does not replace Nexus Mods' review. No attempt has been made to bypass quarantine or scanner checks.

Build the custom plugin using BUILD.md. The original release was not configured as a deterministic build, so a rebuild may differ in byte hash despite identical source and managed behavior. The published release hash identifies the submitted binary; the source hash identifies the exact source copy used.
