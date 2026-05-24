using Bindito.Core;

namespace Spycho.FloodSeason;

// Bindito uses a separate container per context (MainMenu, Game, MapEditor).
// FloodSeasonConfigurator only registers in Game, but we want the settings
// slider to appear in the main menu too — so players can configure before
// starting a save. This sibling configurator binds the settings owner in
// the MainMenu container; the modifier and TemplateModule plumbing stay
// Game-only because there are no water sources in the main menu.
[Context("MainMenu")]
internal class FloodSeasonMainMenuConfigurator : Configurator {

    protected override void Configure() {
        Bind<FloodSeasonSettings>().AsSingleton();
    }

}
