using Timberborn.ModManagerScene;
using UnityEngine;

namespace Kallikor.FloodSeason;

// IModStarter.StartMod runs once when the game loads our mod, before any
// scene is active. Now that settings live in eMka.ModSettings (loaded by
// Bindito when FloodSeasonSettings binds as a singleton), the only job
// left for the entry point is a load confirmation log.
internal class ModStarter : IModStarter {

    public void StartMod(IModEnvironment modEnvironment) {
        Debug.Log("[Flood Season] mod loaded");
    }

}
