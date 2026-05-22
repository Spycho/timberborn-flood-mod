using Timberborn.ModManagerScene;
using UnityEngine;

namespace Kallikor.FloodSeason;

// IModStarter.StartMod runs once when the game loads our mod, before any
// scene is active. This is the entry point — equivalent to Main() for a mod.
internal class ModStarter : IModStarter {

    public void StartMod(IModEnvironment modEnvironment) {
        Debug.Log("[Flood Season] mod loaded");
    }

}
