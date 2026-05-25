using Timberborn.BaseComponentSystem;
using Timberborn.EntitySystem;
using Timberborn.HazardousWeatherSystem;
using Timberborn.SingletonSystem;
using Timberborn.TickSystem;
using Timberborn.WaterSourceSystem;
using Timberborn.WeatherSystem;

namespace Spycho.FloodSeason;

// Per-WaterSource controller that drives the bad-water fraction during
// a Mixed Tide. Modelled structurally on vanilla
// BadtideWaterSourceContaminationController (see
// decomp/WaterSourceSystem/.../BadtideWaterSourceContaminationController.cs),
// with two substantive differences:
//
//   • Type-checks for MixedTideWeather, not BadtideWeather. Even though
//     MixedTideWeather.Id is the string "BadtideWeather" (id-spoof for
//     id-keyed lookups), the C# type is distinct, so `is MixedTide-
//     Weather` doesn't match real badtide and vice versa. Our controller
//     activates only on our weather; vanilla's controller activates only
//     on vanilla badtide. The two coexist cleanly even if both could
//     theoretically be current (they can't — only one weather per cycle).
//
//   • The contamination value is the USER-CONFIGURED CONSTANT from
//     FloodSeasonSettings.MixedTideContamination (default 30% bad), not
//     vanilla's HyperbolicSecant ramp-peak-ramp curve. This is the
//     headline mechanic of the mod's second hazard: a tunable midpoint
//     between drought (no contamination) and badtide (full contamination).
//     We still SetContamination every tick so a later system that resets
//     it (or that gets composed in) can't strand a source at the vanilla
//     default mid-tide.
//
// Wired in FloodSeasonConfigurator via TemplateModule.AddDecorator<
// WaterSource, MixedTideContaminationController>(). Trigger is WaterSource
// itself (not BadtideWaterSourceContaminationControllerSpec, which is
// internal and unreachable from a mod assembly) — same trigger as
// FloodSeasonWaterStrengthModifier, so the Mixed Tide effect applies to
// every water source the existing flow modifier already touches.
//
// WaterSourceContamination is fetched in Awake via GetComponent — every
// WaterSource has it (vanilla WaterSource.Awake itself does
// `GetComponent<WaterSourceContamination>()` and uses it without a null
// check, so it's guaranteed present).
internal class MixedTideContaminationController
    : TickableComponent, IAwakableComponent, IInitializableEntity {

    private readonly EventBus _eventBus;
    private readonly WeatherService _weatherService;
    private readonly HazardousWeatherService _hazardousWeatherService;
    private readonly FloodSeasonSettings _settings;

    private WaterSourceContamination _waterSourceContamination = null!;

    public MixedTideContaminationController(
            EventBus eventBus,
            WeatherService weatherService,
            HazardousWeatherService hazardousWeatherService,
            FloodSeasonSettings settings) {
        _eventBus = eventBus;
        _weatherService = weatherService;
        _hazardousWeatherService = hazardousWeatherService;
        _settings = settings;
    }

    public void Awake() {
        _waterSourceContamination = GetComponent<WaterSourceContamination>();
        _eventBus.Register(this);
    }

    // Mirrors vanilla badtide controller. Important on save-load: when
    // restoring a save that was mid-mixed-tide, our state-persistence
    // PostLoad sets CurrentCycleHazardousWeather to MixedTideWeather
    // BEFORE entities initialise (PostLoad runs after all Loads, but
    // entity init happens during the live-game phase that follows).
    // Without this check, an entity created mid-tide would stay
    // DisableComponent'd and silently emit clean water until the next
    // started-event (which doesn't fire for the cycle that's already
    // running). The vanilla badtide controller carries the same check
    // for the same reason.
    public void InitializeEntity() {
        if (IsMixedTideActive()) {
            EnableComponent();
            UpdateContamination();
        } else {
            DisableComponent();
        }
    }

    public override void Tick() {
        UpdateContamination();
    }

    [OnEvent]
    public void OnHazardousWeatherStarted(HazardousWeatherStartedEvent ev) {
        if (ev.HazardousWeather is MixedTideWeather) {
            EnableComponent();
        }
    }

    [OnEvent]
    public void OnHazardousWeatherEnded(HazardousWeatherEndedEvent ev) {
        if (ev.HazardousWeather is MixedTideWeather) {
            _waterSourceContamination.ResetContamination();
            DisableComponent();
        }
    }

    // Wakes up the controller after HazardousWeatherStatePersistence
    // PostLoad restores CurrentCycleHazardousWeather to MixedTideWeather.
    //
    // The race: vanilla HazardousWeatherService.Load restores the cycle
    // as Badtide (its bool save schema can't represent our custom
    // weather), then entity init runs — InitializeEntity sees current is
    // not MixedTideWeather and disables this controller. Then our
    // PostLoad overrides CurrentCycleHazardousWeather to MixedTideWeather
    // and posts HazardousWeatherSelectedEvent. Without this subscriber,
    // nothing re-enables the controller and water stays uncontaminated
    // for the rest of the loaded hazard.
    //
    // Why this handler is safe (i.e. doesn't fire spuriously when vanilla
    // SetForCycle picks our weather at cycle start): SetForCycle is
    // called at cycle START, which is the TEMPERATE phase of that cycle.
    // At that moment WeatherService.IsHazardousWeather is false. The
    // guard below treats that as "no, don't enable yet" — the controller
    // stays asleep until OnHazardousWeatherStarted fires when the
    // hazardous half actually begins. Our PostLoad fires Selected with
    // IsHazardousWeather already true (because we're loading mid-
    // hazard), so this handler enables immediately.
    [OnEvent]
    public void OnHazardousWeatherSelected(HazardousWeatherSelectedEvent ev) {
        if (ev.SelectedWeather is MixedTideWeather && _weatherService.IsHazardousWeather) {
            EnableComponent();
            UpdateContamination();
        }
    }

    private void UpdateContamination() {
        // FloodSeasonSettings clamps to [0, 1] at the accessor, and
        // WaterSourceContamination.SetContamination clamps to [_, 1]
        // again — double-clamped but cheap.
        _waterSourceContamination.SetContamination(_settings.MixedTideContamination);
    }

    private bool IsMixedTideActive() {
        return _weatherService.IsHazardousWeather
            && _hazardousWeatherService.CurrentCycleHazardousWeather is MixedTideWeather;
    }

}
