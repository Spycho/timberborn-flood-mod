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
internal class FloodSeasonSettings : ModSettingsOwner {

    protected override string ModId => "Kallikor.FloodSeason";

    // ChangeableOn controls *where* the slider is interactive. Including
    // Game means players can crank the multiplier mid-save and see flow
    // adjust the next tick — no restart, no reload.
    public override ModSettingsContext ChangeableOn =>
        ModSettingsContext.MainMenu | ModSettingsContext.Game;

    // Stored as integer percent (200 = 2.0x) because the Mod Settings
    // library only ships a slider widget for ints (RangeIntModSetting).
    // The modifier divides by 100 at read time.
    public RangeIntModSetting MultiplierPercent { get; } = new RangeIntModSetting(
        defaultValue: 200,
        minValue: 10,
        maxValue: 1000,
        ModSettingDescriptor
            .Create("Wet season flow multiplier (%)")
            .SetTooltip("During the Temperate phase, water sources emit at this percentage of their normal strength. 200 means 2× flow. Set to 100 to disable the mod's effect."));

    public FloodSeasonSettings(
        ISettings settings,
        ModSettingsOwnerRegistry registry,
        ModRepository modRepository)
        : base(settings, registry, modRepository) {
    }

    // Convenience accessor so the modifier doesn't have to know about the
    // percent-to-multiplier conversion.
    public float CurrentMultiplier => MultiplierPercent.Value / 100f;

}
