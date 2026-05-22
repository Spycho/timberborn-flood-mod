using Timberborn.ModManagerScene;
using UnityEngine;

namespace Kallikor.FloodSeason;

// IModStarter.StartMod runs once when the game loads our mod, before any
// scene is active. This is the entry point — equivalent to Main() for a mod.
// We use it to read settings.json into FloodSeasonSettings before Bindito's
// game-scene configurator wires up the per-entity modifiers.
internal class ModStarter : IModStarter {

    public void StartMod(IModEnvironment modEnvironment) {
        FloodSeasonSettings.Load(modEnvironment.ModPath);
        Debug.Log("[Flood Season] mod loaded");
    }

}
