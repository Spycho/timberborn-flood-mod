using Timberborn.BaseComponentSystem;
using Timberborn.EntitySystem;
using Timberborn.WaterSourceSystem;
using Timberborn.WeatherSystem;
using UnityEngine;

namespace Kallikor.FloodSeason;

// Per-entity component attached to every WaterSource by the TemplateModule
// decorator wired in FloodSeasonConfigurator. The game multiplies our
// GetStrengthModifier() return value into the source's flow rate every tick
// (see WaterSource.UpdateCurrentStrength). The drought modifier shipped
// with the game uses this same interface to reduce flow during droughts.
//
// Only amplify while the cycle is in its Temperate phase. When IsHazardous-
// Weather flips true (Drought or Badtide), return 1.0 so the game's own
// hazardous-weather modifiers (e.g. DroughtWaterStrengthModifier) own the
// flow behaviour for that phase. Returning the multiplier during a drought
// would fight the game's intended dry-up.
internal class FloodSeasonWaterStrengthModifier
    : BaseComponent, IAwakableComponent, IInitializableEntity, IWaterStrengthModifier {

    private readonly WeatherService _weatherService;

    // Set in Awake, used after — null-forgiving because the framework
    // guarantees Awake runs before InitializeEntity / GetStrengthModifier.
    private WaterSource _waterSource = null!;

    // Bindito injects WeatherService via constructor — the same DI container
    // that wires the rest of the game's services. Singleton; one instance
    // shared across every modifier on the map.
    public FloodSeasonWaterStrengthModifier(WeatherService weatherService) {
        _weatherService = weatherService;
    }

    public void Awake() {
        _waterSource = GetComponent<WaterSource>();
    }

    public void InitializeEntity() {
        _waterSource.AddWaterStrengthModifier(this);
        Debug.Log($"[Flood Season] modifier attached (specified strength {_waterSource.SpecifiedStrength})");
    }

    public float GetStrengthModifier() {
        return _weatherService.IsHazardousWeather ? 1.0f : FloodSeasonSettings.Multiplier;
    }

}
