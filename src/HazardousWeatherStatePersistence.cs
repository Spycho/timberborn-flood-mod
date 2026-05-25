using HarmonyLib;
using Timberborn.HazardousWeatherSystem;
using Timberborn.Persistence;
using Timberborn.SingletonSystem;
using Timberborn.WorldPersistence;
using UnityEngine;

namespace Spycho.FloodSeason;

// Saves and restores which custom hazard (Flood or Mixed Tide) was
// active when the player saved, so loading a save mid-hazard restores
// the correct weather rather than collapsing to vanilla badtide/drought.
//
// THE PROBLEM. The vanilla HazardousWeatherService persists a single
// boolean — `IsDrought: true` for Drought, `false` for Badtide. There's
// no room in that schema for our two extra states, so a save made
// during a flood or mixed tide silently loads back as whichever vanilla
// hazard matches the bool. Without this class, the flood/mixed-tide
// vanishes on every reload.
//
// THE SHAPE OF THE FIX. We maintain our own singleton save key with
// two property bools — `IsFloodActive`, `IsMixedTideActive`. On Save,
// we write whichever bool matches the C# type of the current weather
// (only one can be true at any time; one weather per cycle). On
// PostLoad, if either bool is set, we force-override
// HazardousWeatherService.CurrentCycleHazardousWeather (private
// setter, reached via AccessTools.PropertySetter) to the matching
// custom weather, then re-fire HazardousWeatherSelectedEvent + a fake
// HazardousWeatherEndedEvent(BadtideWeather) so the UI helper
// refreshes and any badtide-contamination controller that vanilla
// load wrongly enabled gets cleaned up.
//
// LIFECYCLE INTERFACES.
//   • ILoadableSingleton with empty Load() — forces Bindito eager
//     construction (see the lazy-AsSingleton gotcha in CLAUDE.md).
//     Without this, nothing injects the class and Save/PostLoad never
//     fire.
//   • ISaveableSingleton — Save() called on every game save.
//   • IPostLoadableSingleton — PostLoad() called AFTER every
//     ILoadableSingleton.Load() has run, including the vanilla
//     HazardousWeatherService.Load() that we need to override. Order
//     matters: doing the restore in Load() would race the vanilla one.
//
// SAVE COMPATIBILITY. The singleton key is
// "Spycho.FloodSeason.HazardousWeatherState" — different from the
// flood-only "Spycho.FloodSeason.FloodWeatherState" we used before
// Mixed Tide landed. Saves made under the old key won't restore a
// flood on load. Acceptable since the mod isn't shipped publicly yet;
// would need a migration shim before any 1.0.0 release.
internal class HazardousWeatherStatePersistence
    : ILoadableSingleton, ISaveableSingleton, IPostLoadableSingleton {

    private static readonly SingletonKey StateKey =
        new SingletonKey("Spycho.FloodSeason.HazardousWeatherState");

    private static readonly PropertyKey<bool> IsFloodActiveKey =
        new PropertyKey<bool>("IsFloodActive");

    private static readonly PropertyKey<bool> IsMixedTideActiveKey =
        new PropertyKey<bool>("IsMixedTideActive");

    private readonly HazardousWeatherService _hazardousWeatherService;
    private readonly ISingletonLoader _singletonLoader;
    private readonly EventBus _eventBus;
    private readonly BadtideWeather _badtideWeather;

    public HazardousWeatherStatePersistence(
            HazardousWeatherService hazardousWeatherService,
            ISingletonLoader singletonLoader,
            EventBus eventBus,
            BadtideWeather badtideWeather) {
        _hazardousWeatherService = hazardousWeatherService;
        _singletonLoader = singletonLoader;
        _eventBus = eventBus;
        _badtideWeather = badtideWeather;
        Debug.Log("[Flood Season] HazardousWeatherStatePersistence constructed");
    }

    // Empty — only here to force eager construction via Bindito's
    // singleton lifecycle.
    public void Load() {
    }

    public void Save(ISingletonSaver singletonSaver) {
        var current = _hazardousWeatherService.CurrentCycleHazardousWeather;
        Debug.Log($"[Flood Season] Save() called, current weather is {current?.GetType().Name ?? "null"} (id={current?.Id})");
        bool isFlood = current is FloodWeather;
        bool isMixed = current is MixedTideWeather;
        if (!isFlood && !isMixed) {
            // Not our weather — don't write the singleton at all. Absence
            // of the key on load means "vanilla cycle, nothing to do".
            return;
        }
        // Always write BOTH keys (exactly one true). Earlier draft wrote
        // only the active bool; the load path then crashed reading the
        // missing one with "Property not found: 'IsFloodActive'" (or
        // 'IsMixedTideActive'). IObjectLoader.Get(PropertyKey<bool>)
        // throws on missing keys rather than returning false, so the
        // schema needs both keys present whenever the singleton is
        // present.
        var saver = singletonSaver.GetSingleton(StateKey);
        saver.Set(IsFloodActiveKey, isFlood);
        saver.Set(IsMixedTideActiveKey, isMixed);
        Debug.Log($"[Flood Season] wrote IsFloodActive={isFlood}, IsMixedTideActive={isMixed} to save");
    }

    public void PostLoad() {
        Debug.Log("[Flood Season] PostLoad() called");
        if (!_singletonLoader.TryGetSingleton(StateKey, out var loader)) {
            Debug.Log("[Flood Season] PostLoad: no hazardous-state singleton in save (not a flood / mixed-tide save)");
            return;
        }
        // Guard every read with Has() — handles two cases:
        //   1. Forward compatibility with future schema additions (an
        //      old build's loader can skip new keys silently).
        //   2. The intermediate single-key-write saves produced before
        //      Save() was fixed to always write both bools. Without
        //      these guards, loading one of those saves crashes with
        //      "Property not found: 'IsFloodActive'" from
        //      SerializedObject.Get.
        bool isFloodActive = loader.Has(IsFloodActiveKey) && loader.Get(IsFloodActiveKey);
        bool isMixedTideActive = loader.Has(IsMixedTideActiveKey) && loader.Get(IsMixedTideActiveKey);
        IHazardousWeather? restore = null;
        if (isFloodActive) {
            restore = FloodWeather.Instance;
            if (restore == null) {
                Debug.LogWarning("[Flood Season] PostLoad: IsFloodActive=true but FloodWeather.Instance is null — cannot restore");
                return;
            }
        } else if (isMixedTideActive) {
            restore = MixedTideWeather.Instance;
            if (restore == null) {
                Debug.LogWarning("[Flood Season] PostLoad: IsMixedTideActive=true but MixedTideWeather.Instance is null — cannot restore");
                return;
            }
        }
        if (restore == null) {
            Debug.Log("[Flood Season] PostLoad: state-key present but neither bool true; nothing to restore");
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
        setter.Invoke(_hazardousWeatherService, new object[] { restore });
        var afterSet = _hazardousWeatherService.CurrentCycleHazardousWeather;
        Debug.Log($"[Flood Season] PostLoad: restored {restore.GetType().Name}. CurrentCycleHazardousWeather is now {afterSet?.GetType().Name} (id={afterSet?.Id})");

        // Re-fire HazardousWeatherSelectedEvent so HazardousWeatherUIHelper
        // re-reads CurrentCycleHazardousWeather and refreshes the cached
        // UI spec. Otherwise the helper's _currentUISpecification stays
        // at whatever vanilla Load set it to (drought or badtide,
        // depending on the bool) — the player sees the right behaviour
        // but wrong labels and icons.
        _eventBus.Post(new HazardousWeatherSelectedEvent(restore, _hazardousWeatherService.HazardousWeatherDuration));
        Debug.Log("[Flood Season] PostLoad: re-fired HazardousWeatherSelectedEvent so UI helper refreshes");

        // Fire a fake HazardousWeatherEndedEvent carrying the vanilla
        // BadtideWeather instance. Vanilla HazardousWeatherService.Load
        // restored the cycle as drought OR badtide (its boolean schema
        // can't represent our custom states), so by the time entities
        // initialised, every BadtideWaterSourceContamination-
        // Controller saw IsBadtideWeather=true (in the badtide-restore
        // case) and enabled itself — contaminating water sources during
        // what should be a flood (or a mixed tide with a different
        // contamination level). Posting EndedEvent(badtide) walks every
        // controller through OnHazardousWeatherEnded, which type-checks
        // `is BadtideWeather` (true), calls ResetContamination() and
        // DisableComponent(). For mixed-tide saves we still need this
        // because MixedTideContaminationController will then re-enable
        // itself on InitializeEntity / next tick using
        // MixedTideContamination%, not vanilla 100%.
        //
        // MusicPlayerEndedPatch intercepts the music handler's
        // subscriber to this event so the audio doesn't double-play.
        _eventBus.Post(new HazardousWeatherEndedEvent(_badtideWeather));
        Debug.Log("[Flood Season] PostLoad: posted fake EndedEvent(BadtideWeather) to reset wrongly-enabled contamination controllers");
    }

}
