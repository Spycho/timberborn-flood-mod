using Timberborn.Common;
using Timberborn.HazardousWeatherSystem;

namespace Spycho.FloodSeason;

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
    private readonly IRandomNumberGenerator _randomNumberGenerator;
    private readonly HazardousWeatherService _hazardousWeatherService;

    public string Id => "DroughtWeather";

    // Convenience for art-overlay patches that need to know whether to
    // swap to flood textures. Cheaper and easier than rebinding
    // HazardousWeatherService into every patch via field injection.
    public bool IsCurrent =>
        _hazardousWeatherService.CurrentCycleHazardousWeather is FloodWeather;

    public FloodWeather(
        FloodSeasonSettings settings,
        IRandomNumberGenerator randomNumberGenerator,
        HazardousWeatherService hazardousWeatherService) {
        _settings = settings;
        _randomNumberGenerator = randomNumberGenerator;
        _hazardousWeatherService = hazardousWeatherService;
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

    // Mirrors how BadtideWeather/DroughtWeather pick a duration: roll a
    // uniform float in [min, max] and round to the nearest day. We skip
    // the vanilla handicap multiplier (shorter on early cycles, longer
    // later) — it's a knob we don't expose, and a predictable range is
    // easier to reason about while tuning.
    //
    // IRandomNumberGenerator.Range(float, float) is inclusive on both
    // ends, matching the variable names in BadtideWeather.cs.
    // Clamp min into [0, max] so a user typo (min > max) collapses to a
    // single-value range instead of throwing inside Range().
    public int GetDurationAtCycle(int cycle) {
        int min = System.Math.Max(0, _settings.FloodDurationMinDays.Value);
        int max = System.Math.Max(min, _settings.FloodDurationMaxDays.Value);
        int duration = (int)System.Math.Round(
            _randomNumberGenerator.Range((float)min, (float)max),
            System.MidpointRounding.AwayFromZero);
        if (min > 0) {
            duration = System.Math.Max(duration, 1);
        }
        return duration;
    }

}
