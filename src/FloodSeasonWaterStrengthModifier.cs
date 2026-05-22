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
// Each weather phase reads its own knob from settings:
//   Temperate → TemperateMultiplier (multiplicative; default 2.0×)
//   Drought   → handled by a Harmony patch on the game's own
//               DroughtWaterStrengthModifier (additive floor; see
//               DroughtFloorPatch). This modifier returns 1.0 during
//               drought because the patch is doing the work.
//   Badtide   → BadtideMultiplier   (multiplicative; default 1.0×)
//   Flood     → FloodMultiplier     (multiplicative; default 5.0×)
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
            BadtideWeather _ => _settings.BadtideMultiplier,
            // Drought is owned by DroughtFloorPatch (additive bonus on
            // top of the game's own DroughtWaterStrengthModifier); we
            // return 1.0 here so we don't double-modify it.
            _ => 1.0f,
        };
    }

}
