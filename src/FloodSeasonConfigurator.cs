using Bindito.Core;
using Timberborn.TemplateInstantiation;
using Timberborn.WaterSourceSystem;

namespace Spycho.FloodSeason;

// Bindito (the game's DI container) auto-discovers this class because of the
// [Context("Game")] attribute, then calls Configure() during in-game scene
// setup. We do two things here:
//
//   1. Register the modifier so Bindito can instantiate it on demand.
//   2. Contribute a TemplateModule that tells the entity system: "for every
//      WaterSource component you spawn, also attach a FloodSeason modifier."
//
// This mirrors how WaterSourceSystemConfigurator (in the base game) wires
// DroughtWaterStrengthModifier into every water source.
[Context("Game")]
internal class FloodSeasonConfigurator : Configurator {

    protected override void Configure() {
        // ModSettingsOwner is ILoadableSingleton — binding as singleton is
        // what triggers Load(), which registers the settings with Mod
        // Settings' registry so they appear in the in-game UI.
        Bind<FloodSeasonSettings>().AsSingleton();

        // One FloodWeather instance per game session. Its constructor
        // publishes itself to FloodWeather.Instance for the Harmony patch
        // to read. Game-only — there's no hazardous weather in main menu.
        Bind<FloodWeather>().AsSingleton();

        // Same lifecycle story for MixedTideWeather — a separate custom
        // IHazardousWeather that the randomizer postfix can pick via
        // MixedTideWeather.Instance.
        Bind<MixedTideWeather>().AsSingleton();

        // Save/load preservation of whichever custom hazard
        // (Flood or Mixed Tide) was active. Implements both
        // ISaveableSingleton (Save() called when the game writes a
        // save) and IPostLoadableSingleton (PostLoad() called after the
        // vanilla HazardousWeatherService.Load() has run).
        Bind<HazardousWeatherStatePersistence>().AsSingleton();

        Bind<FloodSeasonWaterStrengthModifier>().AsTransient();
        // Per-entity component, one instance per WaterSource. Transient
        // because each entity gets its own — singletons would share
        // state across every water source. Without this binding, the
        // TemplateModule decorator below can attach the component to
        // entities but Bindito can't construct one, and the game
        // crashes with BinditoException at first water-source preview
        // build (during BottomBarPanel.Load).
        Bind<MixedTideContaminationController>().AsTransient();
        MultiBind<TemplateModule>().ToProvider(ProvideTemplateModule).AsSingleton();
    }

    private static TemplateModule ProvideTemplateModule() {
        var builder = new TemplateModule.Builder();
        builder.AddDecorator<WaterSource, FloodSeasonWaterStrengthModifier>();
        // Mirrors WaterSourceSystemConfigurator's wiring of
        // BadtideWaterSourceContaminationController, but triggered on
        // WaterSource itself rather than BadtideWaterSourceContamination-
        // ControllerSpec (which is internal and unreachable from a mod
        // assembly). All water sources have WaterSourceContamination,
        // so the broader trigger is safe — the controller's own
        // type-check on CurrentCycleHazardousWeather gates whether
        // contamination actually changes per cycle.
        builder.AddDecorator<WaterSource, MixedTideContaminationController>();
        return builder.Build();
    }

}
