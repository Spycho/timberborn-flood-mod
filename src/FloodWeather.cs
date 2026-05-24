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
// IMPORTANT: Id returns "DroughtWeather" — the SAME string as the game's
// own DroughtWeather. That spoof side-steps every id-based lookup in
// vanilla code that would otherwise throw on an unknown id. Notably:
//   • Sun.GetFogSettings (string id → FogSettingsSpec dictionary lookup)
//     would throw "Weather fog settings not found" — the blueprint data
//     ships fogs only for "DroughtWeather" and "BadtideWeather".
//   • Any future id-keyed lookup in mods or DLC.
// Type-based dispatch (`is FloodWeather` vs `is DroughtWeather`) still
// works correctly because that uses C# type identity, not the string.
// Our own modifier and save-restore logic uses type checks, so it can
// still tell flood and drought apart.
// Side effect: HazardousWeatherHistory.GetCyclesCount("DroughtWeather")
// counts our floods alongside actual droughts, which affects vanilla
// drought's handicap math very mildly. Acceptable for now.
internal class FloodWeather : IHazardousWeather, Timberborn.SingletonSystem.ILoadableSingleton {

    public static FloodWeather? Instance { get; private set; }

    private readonly FloodSeasonSettings _settings;

    public string Id => "DroughtWeather";

    public FloodWeather(FloodSeasonSettings settings) {
        _settings = settings;
        Instance = this;
        UnityEngine.Debug.Log("[Flood Season] FloodWeather constructed; Instance set");
    }

    // Empty — implemented so Bindito's singleton lifecycle eagerly
    // constructs this object during the load phase. Without it, nothing
    // injects FloodWeather (its only consumer is the static Harmony
    // patch), and Bindito's lazy AsSingleton means the instance might
    // never be created. Then FloodWeather.Instance stays null and our
    // randomizer patch silently no-ops.
    public void Load() {
    }

    // The game's own Drought/Badtide use this to scale duration with a
    // handicap multiplier (shorter on early occurrences, longer later).
    // We just take the duration straight from settings — predictability
    // is more useful here than handicap math.
    public int GetDurationAtCycle(int cycle) {
        return _settings.FloodDurationDays.Value;
    }

}
