using ModSettings.Common;
using ModSettings.Core;
using Timberborn.Modding;
using Timberborn.SettingsSystem;

namespace Kallikor.FloodSeason;

// Provides the in-game Mod Settings panel entry for this mod. Subclasses
// ModSettingsOwner from eMka.ModSettings (declared as a RequiredMods entry
// in manifest.json), which reflects over our public ModSetting properties
// and renders matching UI controls in the main menu and in-game settings.
//
// ModSettingsOwner is ILoadableSingleton, so Bindito calls Load() once the
// configurator binds us. Persistence routes through Timberborn.ISettings
// (a PlayerPrefs wrapper) keyed on "ModSetting.{ModId}.{ClassName}.{Prop}".
//
// All multipliers are stored as integer percent (200 = 2.0×) because the
// Mod Settings library only ships a slider widget for ints. The
// convenience accessors below convert to float at read time.
internal class FloodSeasonSettings : ModSettingsOwner {

    // Published in the constructor so the Harmony randomizer patch (a
    // static method that can't take DI) has a way to read the current
    // settings values. The Game-context instance overwrites whatever
    // MainMenu set first; that's fine because both instances are backed
    // by the same PlayerPrefs values.
    public static FloodSeasonSettings? Instance { get; private set; }

    protected override string ModId => "Kallikor.FloodSeason";

    // ChangeableOn controls *where* sliders are interactive. Including
    // Game means players can drag mid-save and see flow adjust on the
    // next tick — no restart, no reload.
    public override ModSettingsContext ChangeableOn =>
        ModSettingsContext.MainMenu | ModSettingsContext.Game;

    public RangeIntModSetting TemperateMultiplierPercent { get; } = new RangeIntModSetting(
        defaultValue: 200,
        minValue: 10,
        maxValue: 1000,
        ModSettingDescriptor
            .Create("Temperate flow multiplier (%)")
            .SetTooltip("Water source flow during the Temperate (non-hazardous) phase. 200 means 2× normal flow."));

    public RangeIntModSetting DroughtMultiplierPercent { get; } = new RangeIntModSetting(
        defaultValue: 100,
        minValue: 10,
        maxValue: 500,
        ModSettingDescriptor
            .Create("Drought flow multiplier (%)")
            .SetTooltip("Stacks on top of the game's natural drought dry-up. 100 leaves drought untouched; 200 makes drought half as severe; 50 makes it twice as harsh."));

    public RangeIntModSetting BadtideMultiplierPercent { get; } = new RangeIntModSetting(
        defaultValue: 100,
        minValue: 10,
        maxValue: 500,
        ModSettingDescriptor
            .Create("Badtide flow multiplier (%)")
            .SetTooltip("Scales water source flow during badtide. The game doesn't reduce flow during badtide by default (only contamination), so this is a pure multiplier."));

    public RangeIntModSetting FloodMultiplierPercent { get; } = new RangeIntModSetting(
        defaultValue: 500,
        minValue: 100,
        maxValue: 2000,
        ModSettingDescriptor
            .Create("Flood flow multiplier (%)")
            .SetTooltip("Water source flow during a flood season. Default 500 means 5× normal flow — the player will need to manage the surplus or watch their settlement get washed out."));

    public RangeIntModSetting FloodDurationDays { get; } = new RangeIntModSetting(
        defaultValue: 3,
        minValue: 1,
        maxValue: 10,
        ModSettingDescriptor
            .Create("Flood duration (days)")
            .SetTooltip("How many in-game days a flood season lasts when it occurs."));

    public ModSetting<bool> FloodSeasonEnabled { get; } = new ModSetting<bool>(
        defaultValue: false,
        ModSettingDescriptor
            .Create("Enable flood season")
            .SetTooltip("Off by default. When on, hazardous cycles may be replaced by a flood instead of the usual drought or badtide."));

    public RangeIntModSetting FloodProbabilityPercent { get; } = new RangeIntModSetting(
        defaultValue: 30,
        minValue: 0,
        maxValue: 100,
        ModSettingDescriptor
            .Create("Flood probability (%)")
            .SetTooltip("Chance per hazardous cycle that a flood replaces the vanilla drought/badtide. 100 means every hazard is a flood; 0 disables (same as turning the feature off)."));

    public RangeIntModSetting FloodGraceCycles { get; } = new RangeIntModSetting(
        defaultValue: 5,
        minValue: 0,
        maxValue: 30,
        ModSettingDescriptor
            .Create("Flood grace cycles")
            .SetTooltip("Number of cycles that must pass before a flood can occur. Lets the player settle in before the new hazard kicks in — mirrors the vanilla 'cycles before badtide' setting."));

    public FloodSeasonSettings(
        ISettings settings,
        ModSettingsOwnerRegistry registry,
        ModRepository modRepository)
        : base(settings, registry, modRepository) {
        Instance = this;
    }

    // Convenience accessors so the modifier doesn't have to know about
    // the percent-to-multiplier conversion at every call site.
    public float TemperateMultiplier => TemperateMultiplierPercent.Value / 100f;
    public float DroughtMultiplier => DroughtMultiplierPercent.Value / 100f;
    public float BadtideMultiplier => BadtideMultiplierPercent.Value / 100f;
    public float FloodMultiplier => FloodMultiplierPercent.Value / 100f;

}
