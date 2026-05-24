# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Timberborn behaviour mod. The repo IS the mod folder — the game loads `Code.dll` and `manifest.json` from this exact directory under `Documents\Timberborn\Mods\Flood Season\`. There's no separate "install" step; building drops the DLL where the game reads it.

Backing material for a tech-guild talk (2026-09-10) on Timberborn modding. Commit messages, file layout, and per-file comments are deliberately slide-quality — preserve that when editing.

## Build

```bash
dotnet build FloodSeason.csproj
```

That's it. `Code.dll` ends up at the mod root via an `AfterBuild` target in the csproj. The game picks it up on next launch.

- Target framework is `netstandard2.1` (Unity 6 Mono runtime). Don't change this — runtime compatibility, not SDK version, depends on it.
- SDK version is .NET 10 LTS. Older SDKs work for compiling but the project assumes 10.
- After a build, verify exactly one `Code.dll` lives inside the mod folder: `find . -name "*.dll"` (PowerShell: `Get-ChildItem -Recurse -Filter *.dll`). More than one means something has gone wrong (see Critical gotchas).

## Run / test

There is no test suite. The mod is verified by:

1. Launching Timberborn.
2. Confirming `Flood Season vX.Y.Z` appears in the Modded list with `eMka.ModSettings` + `Harmony` listed as dependencies (the `RequiredMods` entries in `manifest.json` are enforced).
3. Tailing `%LocalAppData%Low\Mechanistry\Timberborn\Player.log` for `[Flood Season] …` lines.

Settings are exposed via the in-game Mod Settings panel (Main Menu and during gameplay).

## Architecture

The mod does two things, each via a different extensibility pattern:

### 1. Water-flow multipliers (the clean seam)

The game exposes `Timberborn.WaterSourceSystem.IWaterStrengthModifier` as a public extension point. `WaterSource.AddWaterStrengthModifier()` is public, and `WaterSource.UpdateCurrentStrength()` multiplies every modifier's return value into the flow per tick. **No Harmony needed for the core mechanic.**

- `src/FloodSeasonWaterStrengthModifier.cs` — implements `IWaterStrengthModifier`. Switches on `HazardousWeatherService.CurrentCycleHazardousWeather` to pick the right multiplier (Temperate / Drought / Badtide / Flood).
- `src/FloodSeasonConfigurator.cs` — Bindito `[Context("Game")]` configurator. Binds the modifier as transient and contributes a `TemplateModule` decorator that attaches one modifier per `WaterSource` entity. Mirrors how the in-tree `DroughtWaterStrengthModifier` is wired in `WaterSourceSystemConfigurator`.
- `src/FloodSeasonMainMenuConfigurator.cs` — sibling configurator that binds `FloodSeasonSettings` in `[Context("MainMenu")]` so the settings slider shows up before a game starts.

### 2. Flood season as a new hazardous weather (where Harmony is unavoidable)

The game's `HazardousWeatherRandomizer.GetRandomWeatherForCycle` is a hardcoded `if/else` between `DroughtWeather` and `BadtideWeather` — no `MultiBind<IHazardousWeather>`, no public extension. Harmony patches are the only path.

- `src/FloodWeather.cs` — implements `IHazardousWeather`. **`Id` returns `"DroughtWeather"`** (the same string as the game's drought), which is deliberate id-spoofing — see Critical gotchas. Has a static `Instance` property so the Harmony patches (which are static methods) can reach the DI-constructed object.
- `src/Patches/RandomizerPatch.cs` — postfix that, when settings + grace + probability gates all pass, overwrites `__result` with our `FloodWeather`. This is the "decision point" — once SetForCycle has chosen a weather, it can't be changed retroactively.
- `src/Patches/WeatherUIHelperPatch.cs` — prefix that substitutes the existing drought UI spec when our flood is current, preventing `HazardousWeatherUIHelper.UpdateCurrentUISpecification` from throwing on an unknown weather type.
- `src/Patches/SoundPlayerPatch.cs` — prefix that silently skips the sound player when our flood starts (its if/else also throws on unknown types).
- `src/Patches/DroughtFloorPatch.cs` — postfix on the game's `DroughtWaterStrengthModifier.GetStrengthModifier` that adds the user's "drought floor %" setting on top (clamped to 1). The drought setting needs additive composition because game's drought modifier returns 0 at peak — multiplicative composition with our own modifier was a no-op.
- `src/FloodWeatherStatePersistence.cs` — `ISaveableSingleton + IPostLoadableSingleton + ILoadableSingleton`. Writes `IsFloodActive=true` under our own save key when current weather is flood; on `PostLoad`, reads it and uses `AccessTools.PropertySetter` to override the vanilla-loaded `CurrentCycleHazardousWeather` (which would otherwise be Drought or Badtide).

### Settings

`src/FloodSeasonSettings.cs` is a `ModSettingsOwner` (from `eMka.ModSettings`). Library reflects over public `ModSetting<T>` properties and renders matching UI controls — `RangeIntModSetting` for sliders, plain `ModSetting<int>` for text inputs, `ModSetting<bool>` for toggles. Persistence routes through Timberborn's `PlayerPrefs`-backed `ISettings`. A static `Instance` field is set in the constructor so static Harmony patches can read live values.

### Entry point

`src/ModStarter.cs` implements `IModStarter` from `Timberborn.ModManagerScene`. The game discovers it via reflection (`AssemblyLoad → GetTypes() → IsAssignableFrom(IModStarter)`). It calls `new Harmony("Spycho.FloodSeason").PatchAll()` to apply every `[HarmonyPatch]` in the assembly. Without this call, the patch attributes are inert.

## Critical gotchas

These are landmines that have actually exploded in this project. Each commit history entry under `git log --grep "fix:"` corresponds to one.

### `bin/` and `obj/` must live outside the mod folder

Timberborn's mod loader does `mod.ModDirectory.Directory.GetFiles("*.dll", SearchOption.AllDirectories)` — **recursive**. Any `Code.dll` left under `bin/` or `obj/` will be loaded as a *second* assembly with the same types, and every `IModStarter` / `Configurator` / `ModSettingsOwner` gets registered twice. Symptom: duplicate sliders, duplicate log lines.

`Directory.Build.props` redirects `BaseOutputPath` and `BaseIntermediateOutputPath` to `Documents\Timberborn\.flood-season-build\`. Must be in `Directory.Build.props`, not the csproj — `BaseIntermediateOutputPath` is read by NuGet before MSBuild reaches the csproj body (MSB3539 warning).

Don't set `<OutputPath>` to the project root. The SDK's `<DefaultItemExcludes>` includes `$(OutputPath)/**`, which silently excludes every `.cs` file under the project from compilation. Build "succeeds" with an empty DLL containing only `AssemblyInfo`. Verify any time the build feels wrong: a healthy DLL is ~15-25 KB; an empty one is ~8 KB.

### Harmony field-injection naming with underscore-prefixed game fields

Harmony's `___fieldName` convention strips exactly three leading underscores and uses the rest verbatim as the field name (`MethodCreatorTools.cs:363`: `Substring("___".Length)`). The game's private fields are all named `_thingService` — leading underscore is part of the name. So a Harmony parameter binding to `_hazardousWeatherService` must be declared `____hazardousWeatherService` (**four** underscores). Three resolves to `hazardousWeatherService` (no leading underscore) and throws `ArgumentException: No such field defined`.

### Bindito `AsSingleton()` is lazy at the binding level

Singletons construct only when something resolves them. If a singleton's job is to listen to lifecycle events (Save / PostLoad) and no code injects it as a dependency, **it never constructs and never fires**. Symptom in this repo: save-time and load-time logic silently skipped.

Fix is to implement at least one `[Singleton]`-tagged interface that `SingletonLifecycleService` actively walks. `ILoadableSingleton` with an empty `Load()` is the simplest. Both `FloodWeather` and `FloodWeatherStatePersistence` carry empty `Load()` methods specifically for eager construction.

### Vanilla type-checks throw on unknown `IHazardousWeather` types

Several game systems hardcode `is DroughtWeather` / `is BadtideWeather` with a `throw` in the else branch. We've patched the ones that fired (`HazardousWeatherUIHelper`, `HazardousWeatherSoundPlayer`). Others exist that haven't triggered yet (notification panel uses the UI helper indirectly so it's fine). When adding flood-active features, search game decompiles for `is DroughtWeather` / `is BadtideWeather` and treat each occurrence as a potential crash site.

### Id-keyed lookups are spoofed via `FloodWeather.Id`

`FloodWeather.Id` returns `"DroughtWeather"` — the literal same string as the game's drought. This silences id-keyed dictionary lookups in vanilla code (`Sun.GetFogSettings` was the one that bit us; any future id-keyed lookup in mods or DLC will also fall back to drought's data). Type-based dispatch still distinguishes flood from drought because that uses C# type identity, not the string.

Side effect: `HazardousWeatherHistory.GetCyclesCount("DroughtWeather")` aggregates our floods alongside actual droughts, which feeds drought's handicap-multiplier math slightly differently. Acceptable in current scope; flag if shipping publicly.

### Cycle weather is decided at the start of each cycle and persisted

`GameCycleService.StartNextCycle()` calls `SetForCycle(N)` once when cycle N starts. The chosen `IHazardousWeather` is then saved in the save file. Loading a save **does not** re-roll — `GameCycleService.Load()` restores the saved `Cycle` field and skips `StartNextCycle()`. Settings changes affect only future cycles. The randomizer postfix intercepts at decision time, not retroactively.

## External dependencies

Both must be present in the player's mod list — `manifest.json` declares them as required. References use `<Private>false</Private>` so we never redistribute them.

- **`eMka.ModSettings`** (Workshop id `3283831040`). Source of `ModSettingsOwner`, `ModSetting<T>`, `RangeIntModSetting`, `ModSettingDescriptor`, `ModSettingsContext`. DLLs at `…\workshop\content\1062090\3283831040\version-1.0\Scripts\`. The `ModSettingsDir` csproj property points here.
- **`Harmony` / Harmony for Timberborn** (Workshop id `3284904751`). Redistributes `0Harmony.dll` (the game itself does not ship Harmony). DLL at `…\workshop\content\1062090\3284904751\0Harmony.dll`. The `HarmonyDir` csproj property points here.

Game DLLs come from `C:\Program Files (x86)\Steam\steamapps\common\Timberborn\Timberborn_Data\Managed\`. The `GameDir`/`ManagedDir` csproj properties resolve them; override on the build line with `dotnet build -p:GameDir=…` if Steam is installed elsewhere.

## Decompiling game code

The codebase consults the game's compiled DLLs extensively. Standard tool here is `ilspycmd` (installed via `dotnet tool install -g ilspycmd`).

Per-system DLL: `ilspycmd -p -o ../decomp/<NAME> "C:/Program Files (x86)/Steam/steamapps/common/Timberborn/Timberborn_Data/Managed/<NAME>.dll"`. The conventional dump location is `Documents\Timberborn\decomp\<DllName>\` — outside the mod folder, not in source control. Game systems are split into many small assemblies (one per system, ~490 in total) so usually only one or two need decompiling for any given question.

## Conventions worth preserving

- Per-phase commits with thorough messages. `git log --oneline` reads like a tutorial — keep it that way.
- Per-file comments explain *why* (especially around Harmony quirks), not *what*.
- Bump `manifest.json` `Version` on each commit that changes behaviour.
- New patches go under `src/Patches/`. New non-patch classes go directly under `src/`.
- Each commit ends in a green build and a demoable in-game change (the user verifies manually after each).
