using Timberborn.BaseComponentSystem;
using Timberborn.EntitySystem;
using Timberborn.HazardousWeatherSystem;
using Timberborn.WaterSourceSystem;
using Timberborn.WeatherSystem;
using UnityEngine;

namespace Kallikor.FloodSeason;

// Per-entity component attached to every WaterSource by the TemplateModule
// decorator wired in FloodSeasonConfigurator. The game multiplies our
// GetStrengthModifier() return value into the source's flow rate every tick
// (see WaterSource.UpdateCurrentStrength). The drought modifier shipped
// with the game uses this same interface to reduce flow during droughts —
// our multiplier composes on top of whatever it returns.
//
// Each weather phase reads its own multiplier from settings so the player
// can tune each one independently:
//   Temperate → TemperateMultiplier  (default 2.0×)
//   Drought   → DroughtMultiplier    (default 1.0× — leave game behaviour alone)
//   Badtide   → BadtideMultiplier    (default 1.0× — leave game behaviour alone)
internal class FloodSeasonWaterStrengthModifier
    : BaseComponent, IAwakableComponent, IInitializableEntity, IWaterStrengthModifier {

    private readonly WeatherService _weatherService;
    private readonly HazardousWeatherService _hazardousWeatherService;
    private readonly FloodSeasonSettings _settings;

    // Set in Awake, used after — null-forgiving because the framework
    // guarantees Awake runs before InitializeEntity / GetStrengthModifier.
    private WaterSource _waterSource = null!;

    public FloodSeasonWaterStrengthModifier(
        WeatherService weatherService,
        HazardousWeatherService hazardousWeatherService,
        FloodSeasonSettings settings) {
        _weatherService = weatherService;
        _hazardousWeatherService = hazardousWeatherService;
        _settings = settings;
    }

    public void Awake() {
        _waterSource = GetComponent<WaterSource>();
    }

    public void InitializeEntity() {
        _waterSource.AddWaterStrengthModifier(this);
        Debug.Log($"[Flood Season] modifier attached (specified strength {_waterSource.SpecifiedStrength})");
    }

    public float GetStrengthModifier() {
        if (!_weatherService.IsHazardousWeather) {
            return _settings.TemperateMultiplier;
        }
        return _hazardousWeatherService.CurrentCycleHazardousWeather switch {
            FloodWeather _   => _settings.FloodMultiplier,
            DroughtWeather _ => _settings.DroughtMultiplier,
            BadtideWeather _ => _settings.BadtideMultiplier,
            // Defensive default for any third-party weather we don't know.
            _ => 1.0f,
        };
    }

}
