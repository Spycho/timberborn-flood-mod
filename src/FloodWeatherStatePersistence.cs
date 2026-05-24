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
// Three lifecycle interfaces:
//
//   ILoadableSingleton (empty Load):
//       Forces Bindito to eagerly construct this object during the load
//       phase, which in turn registers it with Timberborn's
//       SingletonListener (via the [Singleton]-tagged ISaveable/IPostLoad
//       interfaces). Without this, nothing injects this class, so
//       Bindito's lazy singleton wouldn't construct it, so Save and
//       PostLoad would never run.
//
//   ISaveableSingleton:
//       If the current weather is FloodWeather, write IsFloodActive=true
//       under our own singleton key. We only write *true*: absent key
//       means "not a flood", keeping non-flood saves identical to pre-mod
//       saves.
//
//   IPostLoadableSingleton:
//       Runs AFTER every ILoadableSingleton.Load() — including
//       HazardousWeatherService.Load(), which has just set
//       CurrentCycleHazardousWeather to drought/badtide from the vanilla
//       bool. If our key says flood was active, we force-override the
//       property via AccessTools (its setter is private). Order matters:
//       this is why we implement IPostLoadableSingleton rather than
//       ILoadableSingleton for the restore logic.
internal class FloodWeatherStatePersistence
    : ILoadableSingleton, ISaveableSingleton, IPostLoadableSingleton {

    private static readonly SingletonKey FloodSeasonStateKey =
        new SingletonKey("Kallikor.FloodSeason.FloodWeatherState");

    private static readonly PropertyKey<bool> IsFloodActiveKey =
        new PropertyKey<bool>("IsFloodActive");

    private readonly HazardousWeatherService _hazardousWeatherService;
    private readonly ISingletonLoader _singletonLoader;
    private readonly EventBus _eventBus;

    public FloodWeatherStatePersistence(
        HazardousWeatherService hazardousWeatherService,
        ISingletonLoader singletonLoader,
        EventBus eventBus) {
        _hazardousWeatherService = hazardousWeatherService;
        _singletonLoader = singletonLoader;
        _eventBus = eventBus;
        Debug.Log("[Flood Season] FloodWeatherStatePersistence constructed");
    }

    // Empty — only here to force eager construction via Bindito's singleton lifecycle.
    public void Load() {
    }

    public void Save(ISingletonSaver singletonSaver) {
        var current = _hazardousWeatherService.CurrentCycleHazardousWeather;
        Debug.Log($"[Flood Season] Save() called, current weather is {current?.GetType().Name ?? "null"} (id={current?.Id})");
        if (current is FloodWeather) {
            var saver = singletonSaver.GetSingleton(FloodSeasonStateKey);
            saver.Set(IsFloodActiveKey, true);
            Debug.Log("[Flood Season] wrote IsFloodActive=true to save");
        }
    }

    public void PostLoad() {
        Debug.Log("[Flood Season] PostLoad() called");
        if (!_singletonLoader.TryGetSingleton(FloodSeasonStateKey, out var loader)) {
            Debug.Log("[Flood Season] PostLoad: no flood-state singleton in save (not a flood save)");
            return;
        }
        if (!loader.Get(IsFloodActiveKey)) {
            Debug.Log("[Flood Season] PostLoad: flood-state key found but IsFloodActive=false");
            return;
        }
        var flood = FloodWeather.Instance;
        if (flood == null) {
            Debug.LogWarning("[Flood Season] PostLoad: IsFloodActive=true but FloodWeather.Instance is null — cannot restore");
            return;
        }
        // The property is `public IHazardousWeather CurrentCycleHazardousWeather { get; private set; }`
        // — public getter, private setter. AccessTools.PropertySetter
        // bypasses the access check and returns the setter MethodInfo
        // even when it's non-public.
        var setter = AccessTools.PropertySetter(
            typeof(HazardousWeatherService),
            nameof(HazardousWeatherService.CurrentCycleHazardousWeather));
        if (setter == null) {
            Debug.LogWarning("[Flood Season] PostLoad: AccessTools.PropertySetter returned null — cannot restore");
            return;
        }
        setter.Invoke(_hazardousWeatherService, new object[] { flood });
        var afterSet = _hazardousWeatherService.CurrentCycleHazardousWeather;
        Debug.Log($"[Flood Season] PostLoad: restored flood. CurrentCycleHazardousWeather is now {afterSet?.GetType().Name} (id={afterSet?.Id})");

        // Re-fire HazardousWeatherSelectedEvent so HazardousWeatherUIHelper
        // re-reads CurrentCycleHazardousWeather and refreshes the cached
        // UI spec. Otherwise the helper's _currentUISpecification stays at
        // whatever vanilla Load set it to (badtide, since IsDroughtKey
        // defaulted to false) — the player sees flood behaviour but
        // badtide labels and icons. The only HazardousWeatherSelectedEvent
        // subscriber is HazardousWeatherUIHelper.OnHazardousWeatherSelected,
        // so re-firing has no side effects on sound, notifications, or
        // water contamination.
        _eventBus.Post(new HazardousWeatherSelectedEvent(flood, _hazardousWeatherService.HazardousWeatherDuration));
        Debug.Log("[Flood Season] PostLoad: re-fired HazardousWeatherSelectedEvent so UI helper refreshes");
    }

}
