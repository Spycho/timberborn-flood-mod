using Timberborn.BaseComponentSystem;
using Timberborn.EntitySystem;
using Timberborn.WaterSourceSystem;
using UnityEngine;

namespace Kallikor.FloodSeason;

// Per-entity component attached to every WaterSource by the TemplateModule
// decorator wired in FloodSeasonConfigurator. The game multiplies our
// GetStrengthModifier() return value into the source's flow rate every tick
// (see WaterSource.UpdateCurrentStrength). The drought modifier shipped
// with the game uses this same interface to reduce flow during droughts.
//
// This phase: unconditional 2x. Season gating comes in the next commit.
internal class FloodSeasonWaterStrengthModifier
    : BaseComponent, IAwakableComponent, IInitializableEntity, IWaterStrengthModifier {

    private const float Multiplier = 2.0f;

    // Set in Awake, used after — null-forgiving because the framework
    // guarantees Awake runs before InitializeEntity / GetStrengthModifier.
    private WaterSource _waterSource = null!;

    public void Awake() {
        _waterSource = GetComponent<WaterSource>();
    }

    public void InitializeEntity() {
        _waterSource.AddWaterStrengthModifier(this);
        Debug.Log($"[Flood Season] modifier attached (specified strength {_waterSource.SpecifiedStrength})");
    }

    public float GetStrengthModifier() => Multiplier;

}
