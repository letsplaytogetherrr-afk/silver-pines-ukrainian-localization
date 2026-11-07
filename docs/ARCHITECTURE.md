# Architecture relevant to review

Entry point: `LocalizationPlugin : BepInEx.Unity.IL2CPP.BasePlugin`, GUID `skarig.silverpines.localization`, version `0.1.9`.

## Text locale

The plugin reads UTF-8 `texts.json` and `bundles.json` from its own installation directory. After native localization initialization, it retains the English shared-data dependencies and registers an additional ResourceLocationMap for separate Ukrainian tables in `UkrainianTexts.bundle`. The locale is `uk`; Russian and the other original locales are retained. Before exposing the locale, the code tests all 24 tables by name and GUID and checks six main-menu values.

The extra locator contains only qualified localization keys: `<table>_uk`, `Locale-uk`, and `GUID:<collection-guid>`. It does not replace the stock catalog file or register ambiguous generic labels such as `Levels` and `Video`.

Carpenter's own synchronous localization helper is patched to resolve retained Ukrainian entries and invoke the stock formatter with the original arguments. An empty optional table/entry reference falls through to the original helper. Translation errors must not abort input setup or the close handler for an examined object.

## Separate resource packs

`bundles.json` maps exact original bundle names to their baseline SHA256 and full/light translated-copy SHA256 values. Two concrete `AssetBundleResource.GetLoadInfo` overloads redirect only the installed game's exact `StreamingAssets/aa/StandaloneWindows64` paths to plugin-local files. A changed original baseline or corrupt sidecar is skipped with a log message. The CRC override applies only to the redirected local request.

The loader does not rewrite the original bundle, executable, metadata, or Addressables catalog on disk. Texture selection is restricted to the main menu and takes effect on the next full process launch. Both resource variants ship in the release. New game updates are not unconditionally compatible: baseline guards preserve the original resource when the localized copy is no longer compatible.

## Fonts and UI

The data package includes extended copies of the existing Cyrillic font atlases. The plugin preserves native font family/weight mapping through a font-only Russian locale override while text stays Ukrainian. Ready fonts are retained and reused. It does not substitute one global bold font everywhere.

No synchronous font loading or locale construction is performed inside the TMP OnEnable hook. Fonts are asynchronously prewarmed; the hook can apply an already available font. The code avoids detouring shared IL2CPP generic method bodies, which proved unsafe in discarded prototypes.

Approved menu layout and artwork changes live in the separate data packs, not in the C# source. This source repository deliberately omits those game-derived assets.

## Phone and vending display

Phone/vending concrete hooks translate known status literals and source references. Native call handling, digits, blink timing, and vending scroll timing are retained. A concrete generated phone coroutine, `_Dialing_d__81.MoveNext`, also writes directly to the display; its postfix translates the DIALING heading. That generated name is sensitive to game-version changes.

`MinigameHoverText` is disabled only in the Ukrainian-texture mode requested for this release. Other world interaction components are not globally disabled.

## Files, network, and processes

The plugin reads its own data packs, original bundle hashes, and (for a build-label fallback) Steam's `appmanifest_2333000.acf`. It saves its own BepInEx configuration and writes BepInEx logs. The custom plugin does not implement network requests, credential collection, process launching/termination, deletion of game files, or save-file editing.

The third-party BepInEx loader can obtain Unity base libraries when generating interop for an uncached engine version. The public release includes the tested engine's library ZIP as a local cache. Loader behavior should be reviewed separately from the custom plugin.
