using ModSettings.Core;
using Timberborn.Modding;
using Timberborn.SettingsSystem;

namespace Spycho.FloodSeason;

// Provides the in-game Mod Settings panel entry for this mod. Subclasses
// ModSettingsOwner from eMka.ModSettings (declared as a RequiredMods entry
// in manifest.json), which reflects over our public ModSetting properties
// and renders matching UI controls in the main menu and in-game settings.
//
// All numeric settings are plain ModSetting<int> rather than the library's
// RangeIntModSetting. The library's UI factory renders RangeIntModSetting
// as a fixed-step slider (priority 100), and plain ModSetting<int> as an
// IntegerField text input (priority 0). The user prefers text input —
// unbounded, single-unit precision, easy to type "100" exactly. We clamp
// to a sensible floor at read time to keep negative entries from causing
// negative-flow weirdness.
//
// All values are stored as integer percent (200 = 2.0×). The accessor
// converters at the bottom of the class handle the divide-by-100.
internal class FloodSeasonSettings : ModSettingsOwner {

    // Published in the constructor so the Harmony patches (static methods
    // that can't take DI) have a way to read the current settings values.
    public static FloodSeasonSettings? Instance { get; private set; }

    protected override string ModId => "Spycho.FloodSeason";

    public override ModSettingsContext ChangeableOn =>
        ModSettingsContext.MainMenu | ModSettingsContext.Game;

    public ModSetting<int> TemperateMultiplierPercent { get; } = new ModSetting<int>(
        defaultValue: 200,
        ModSettingDescriptor
            .Create("Temperate flow multiplier (%)")
            .SetTooltip("Water source flow during the Temperate (non-hazardous) phase. 200 means 2× normal flow. Set to 100 to disable the effect."));

    // Drought is ADDITIVE, not multiplicative.
    //
    // The game's own DroughtWaterStrengthModifier reduces flow during a
    // drought, transitioning down to 0 at peak drought. Composing our
    // setting multiplicatively meant "0 × anything = 0" — the setting
    // had no effect during peak drought. The fix is to add our value on
    // top of the game's modifier (clamped to [0, 1]), so peak drought
    // can still leak the configured amount of water.
    //
    // 0 = vanilla drought (water can dry up to nothing)
    // 30 = peak drought floor is 30% of specified strength
    // 100 = drought effectively cancelled (flow stays at full)
    public ModSetting<int> DroughtAdditivePercent { get; } = new ModSetting<int>(
        defaultValue: 0,
        ModSettingDescriptor
            .Create("Drought flow floor (%)")
            .SetTooltip("Amount added to the game's drought modifier (clamped to 100%). 0 leaves vanilla drought untouched; 30 keeps water flowing at 30% during peak drought; 100 cancels drought's effect on flow."));

    public ModSetting<int> BadtideMultiplierPercent { get; } = new ModSetting<int>(
        defaultValue: 100,
        ModSettingDescriptor
            .Create("Badtide flow multiplier (%)")
            .SetTooltip("Scales water source flow during badtide. Vanilla badtide contaminates water but doesn't reduce flow, so this is a pure multiplier."));

    public ModSetting<int> FloodMultiplierPercent { get; } = new ModSetting<int>(
        defaultValue: 500,
        ModSettingDescriptor
            .Create("Flood flow multiplier (%)")
            .SetTooltip("Water source flow during a flood season. Default 500 means 5× normal flow."));

    // Min/max pair, picked uniformly at random per flood (rounded to the
    // nearest day). Mirrors the vanilla MinBadtideWeatherDuration /
    // MaxBadtideWeatherDuration knobs. Range is inclusive on both ends.
    // If min > max we treat them as both equal to min at read time.
    public ModSetting<int> FloodDurationMinDays { get; } = new ModSetting<int>(
        defaultValue: 2,
        ModSettingDescriptor
            .Create("Flood duration min (days)")
            .SetTooltip("Lower bound (inclusive) on the number of in-game days a flood season lasts. Each flood rolls a random duration in [min, max]."));

    public ModSetting<int> FloodDurationMaxDays { get; } = new ModSetting<int>(
        defaultValue: 5,
        ModSettingDescriptor
            .Create("Flood duration max (days)")
            .SetTooltip("Upper bound (inclusive) on the number of in-game days a flood season lasts. Each flood rolls a random duration in [min, max]."));

    public ModSetting<bool> FloodSeasonEnabled { get; } = new ModSetting<bool>(
        defaultValue: false,
        ModSettingDescriptor
            .Create("Enable flood season")
            .SetTooltip("Off by default. When on, hazardous cycles may be replaced by a flood instead of the usual drought or badtide."));

    public ModSetting<int> FloodProbabilityPercent { get; } = new ModSetting<int>(
        defaultValue: 30,
        ModSettingDescriptor
            .Create("Flood probability (%)")
            .SetTooltip("Chance per hazardous cycle that a flood replaces the vanilla drought/badtide. 0 disables (same as turning the feature off); 100 means every hazard is a flood."));

    public ModSetting<int> FloodGraceCycles { get; } = new ModSetting<int>(
        defaultValue: 5,
        ModSettingDescriptor
            .Create("Flood grace cycles")
            .SetTooltip("Cycles that must pass before a flood can occur. Mirrors the vanilla 'cycles before badtide' knob."));

    public FloodSeasonSettings(
        ISettings settings,
        ModSettingsOwnerRegistry registry,
        ModRepository modRepository)
        : base(settings, registry, modRepository) {
        Instance = this;
    }

    // Convenience accessors. Each clamps to non-negative because the
    // text inputs are now unbounded and a stray negative entry would
    // otherwise produce reversed or zero flow in weird places.
    public float TemperateMultiplier => System.Math.Max(0, TemperateMultiplierPercent.Value) / 100f;
    public float DroughtAdditive    => System.Math.Max(0, DroughtAdditivePercent.Value)    / 100f;
    public float BadtideMultiplier  => System.Math.Max(0, BadtideMultiplierPercent.Value)  / 100f;
    public float FloodMultiplier    => System.Math.Max(0, FloodMultiplierPercent.Value)    / 100f;

}
