using Timberborn.Common;
using Timberborn.HazardousWeatherSystem;

namespace Spycho.FloodSeason;

// Second custom IHazardousWeather alongside FloodWeather. Slotted into
// the same selection slot the game uses for Drought and Badtide via the
// Harmony postfix on HazardousWeatherRandomizer.GetRandomWeatherForCycle.
//
// Where Flood is a "wetter than wet" exaggeration of the temperate
// phase, Mixed Tide is a tunable midpoint between vanilla badtide
// (water 100% contaminated) and vanilla drought (water 0% contaminated).
// During a mixed tide every water source emits water at a user-set
// contamination ratio (MixedTideContamination, default 30% = 30% bad,
// 70% clean). The mix is real: the value flows into the water-column
// simulation (see decomp/WaterSystem/.../SimulateContamination), so
// downstream tiles read the diffused mixture as actual bad-water
// fraction.
//
// IMPORTANT: Id returns "BadtideWeather" — the same string as the
// game's own BadtideWeather. Same id-spoof rationale as FloodWeather's
// "DroughtWeather" spoof: side-step every id-keyed lookup in vanilla
// code that would otherwise throw on an unknown id (notably
// Sun.GetFogSettings, where blueprint fog data is shipped only for
// "DroughtWeather" and "BadtideWeather"). Type-based dispatch
// (`is MixedTideWeather`) still distinguishes us from BadtideWeather
// because that's C# type identity, not the id string. The contamination
// controller and flow modifier use type checks, so they can tell
// MixedTide from real Badtide.
//
// Side effect: HazardousWeatherHistory.GetCyclesCount("BadtideWeather")
// counts our mixed tides alongside actual badtides — feeds vanilla
// badtide's handicap-multiplier math slightly differently. Same
// acceptable-for-now stance as the Flood case.
internal class MixedTideWeather : IHazardousWeather, Timberborn.SingletonSystem.ILoadableSingleton {

    public static MixedTideWeather? Instance { get; private set; }

    private readonly FloodSeasonSettings _settings;
    private readonly IRandomNumberGenerator _randomNumberGenerator;
    private readonly HazardousWeatherService _hazardousWeatherService;

    public string Id => "BadtideWeather";

    // Convenience for the art-overlay patches and the contamination
    // controller — both want to know whether a mixed tide is currently
    // active without rebinding HazardousWeatherService through their own
    // field injections.
    public bool IsCurrent =>
        _hazardousWeatherService.CurrentCycleHazardousWeather is MixedTideWeather;

    public MixedTideWeather(
            FloodSeasonSettings settings,
            IRandomNumberGenerator randomNumberGenerator,
            HazardousWeatherService hazardousWeatherService) {
        _settings = settings;
        _randomNumberGenerator = randomNumberGenerator;
        _hazardousWeatherService = hazardousWeatherService;
        Instance = this;
        UnityEngine.Debug.Log("[Flood Season] MixedTideWeather constructed; Instance set");
    }

    // Empty — forces eager construction via Bindito's singleton lifecycle
    // (see the lazy-AsSingleton gotcha in CLAUDE.md). The only consumer
    // of MixedTideWeather besides the per-source contamination controller
    // is the static Harmony randomizer postfix, which can't take DI —
    // so if Bindito doesn't eagerly construct, MixedTideWeather.Instance
    // stays null and the randomizer silently never picks us.
    public void Load() {
    }

    // Same shape as FloodWeather.GetDurationAtCycle: uniform random day
    // count in [min, max], rounded to the nearest day, with a floor of 1
    // when min > 0 so a "(1, 2)" range can't collapse to zero. Skips the
    // vanilla per-cycle handicap multiplier for predictability while
    // tuning.
    public int GetDurationAtCycle(int cycle) {
        int min = System.Math.Max(0, _settings.MixedTideDurationMinDays.Value);
        int max = System.Math.Max(min, _settings.MixedTideDurationMaxDays.Value);
        int duration = (int)System.Math.Round(
            _randomNumberGenerator.Range((float)min, (float)max),
            System.MidpointRounding.AwayFromZero);
        if (min > 0) {
            duration = System.Math.Max(duration, 1);
        }
        return duration;
    }

}
