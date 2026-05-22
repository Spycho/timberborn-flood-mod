using Timberborn.HazardousWeatherSystem;

namespace Kallikor.FloodSeason;

// A new IHazardousWeather implementation, slotted into the same selection
// slot the game uses for Drought and Badtide. Discovered by the Harmony
// patch on HazardousWeatherRandomizer.GetRandomWeatherForCycle, which
// substitutes this in for the vanilla choice when conditions are met.
//
// Bindito constructs this as a singleton (see FloodSeasonConfigurator),
// which lets us pull FloodSeasonSettings via DI for the duration value.
// The constructor also publishes a static Instance for the Harmony patch
// to grab — patches are static methods and can't take DI.
//
// The Id string is what HazardousWeatherHistory keys on; it stays stable
// across save/load so the game can track "how many flood seasons have
// occurred". Don't change this casually.
internal class FloodWeather : IHazardousWeather {

    public static FloodWeather? Instance { get; private set; }

    private readonly FloodSeasonSettings _settings;

    public string Id => "Kallikor.FloodWeather";

    public FloodWeather(FloodSeasonSettings settings) {
        _settings = settings;
        Instance = this;
    }

    // The game's own Drought/Badtide use this to scale duration with a
    // handicap multiplier (shorter on early occurrences, longer later).
    // We just take the duration straight from settings — predictability
    // is more useful here than handicap math.
    public int GetDurationAtCycle(int cycle) {
        return _settings.FloodDurationDays.Value;
    }

}
