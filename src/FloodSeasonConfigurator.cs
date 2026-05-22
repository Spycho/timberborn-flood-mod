using Bindito.Core;
using Timberborn.TemplateInstantiation;
using Timberborn.WaterSourceSystem;

namespace Kallikor.FloodSeason;

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

        Bind<FloodSeasonWaterStrengthModifier>().AsTransient();
        MultiBind<TemplateModule>().ToProvider(ProvideTemplateModule).AsSingleton();
    }

    private static TemplateModule ProvideTemplateModule() {
        var builder = new TemplateModule.Builder();
        builder.AddDecorator<WaterSource, FloodSeasonWaterStrengthModifier>();
        return builder.Build();
    }

}
