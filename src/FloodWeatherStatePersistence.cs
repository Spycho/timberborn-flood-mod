using HarmonyLib;
using Timberborn.HazardousWeatherSystem;
using Timberborn.Persistence;
using Timberborn.SingletonSystem;
using Timberborn.WorldPersistence;
using UnityEngine;

namespace Kallikor.FloodSeason;

// Saves and restores the "flood was active when we saved" bit.
//
// The vanilla HazardousWeatherService persists a single boolean
// IsDroughtKey — true for Drought, false for Badtide. There's no room
// in that schema for a third state, so a save made while flood is
// active would load back as Badtide and the flood would vanish. We
// side-step that by maintaining our own singleton save key.
//
// Two-step protocol:
//
//   Save:       if the current weather is FloodWeather, write
//               IsFloodActive=true under our own singleton key. We only
//               write *true*: absent key means "not a flood", which
//               keeps non-flood saves identical to pre-mod saves.
//
//   PostLoad:   runs AFTER every ILoadableSingleton.Load() — including
//               HazardousWeatherService.Load(), which has just set
//               CurrentCycleHazardousWeather to drought/badtide from
//               the vanilla bool. If our key says flood was active, we
//               force-override the property via AccessTools (its setter
//               is private). Order matters: this is why we implement
//               IPostLoadableSingleton rather than ILoadableSingleton.
internal class FloodWeatherStatePersistence
    : ISaveableSingleton, IPostLoadableSingleton {

    private static readonly SingletonKey FloodSeasonStateKey =
        new SingletonKey("Kallikor.FloodSeason.FloodWeatherState");

    private static readonly PropertyKey<bool> IsFloodActiveKey =
        new PropertyKey<bool>("IsFloodActive");

    private readonly HazardousWeatherService _hazardousWeatherService;
    private readonly ISingletonLoader _singletonLoader;

    public FloodWeatherStatePersistence(
        HazardousWeatherService hazardousWeatherService,
        ISingletonLoader singletonLoader) {
        _hazardousWeatherService = hazardousWeatherService;
        _singletonLoader = singletonLoader;
    }

    public void Save(ISingletonSaver singletonSaver) {
        if (_hazardousWeatherService.CurrentCycleHazardousWeather is FloodWeather) {
            var saver = singletonSaver.GetSingleton(FloodSeasonStateKey);
            saver.Set(IsFloodActiveKey, true);
        }
    }

    public void PostLoad() {
        if (!_singletonLoader.TryGetSingleton(FloodSeasonStateKey, out var loader)) {
            return;
        }
        if (!loader.Get(IsFloodActiveKey)) {
            return;
        }
        var flood = FloodWeather.Instance;
        if (flood == null) {
            return;
        }
        // The property is `public IHazardousWeather CurrentCycleHazardousWeather { get; private set; }`
        // — public getter, private setter. AccessTools.PropertySetter
        // bypasses the access check and returns the setter MethodInfo
        // even when it's non-public. More robust than reflecting on the
        // compiler-generated backing field, whose name (`<…>k__BackingField`)
        // could change if the property gets a custom setter later.
        var setter = AccessTools.PropertySetter(
            typeof(HazardousWeatherService),
            nameof(HazardousWeatherService.CurrentCycleHazardousWeather));
        setter?.Invoke(_hazardousWeatherService, new object[] { flood });
        Debug.Log("[Flood Season] restored active flood weather from save");
    }

}
