using System;
using System.IO;
using UnityEngine;

namespace Kallikor.FloodSeason;

// Static settings cache, populated once at mod startup from settings.json
// next to manifest.json. Static (rather than a DI-bound singleton) because
// the file path comes from IModEnvironment.ModPath, which is only handed to
// us inside IModStarter — well before Bindito's game-scene configurator
// runs. Storing it here lets the per-entity modifier read the multiplier
// without an extra layer of plumbing.
internal static class FloodSeasonSettings {

    private const string SettingsFileName = "settings.json";
    private const float DefaultMultiplier = 2.0f;
    private const float MinMultiplier = 0.1f;
    private const float MaxMultiplier = 100f;

    public static float Multiplier { get; private set; } = DefaultMultiplier;

    public static void Load(string modPath) {
        string path = Path.Combine(modPath, SettingsFileName);

        if (!File.Exists(path)) {
            Debug.Log($"[Flood Season] no settings.json at {path}; using default multiplier {DefaultMultiplier}");
            return;
        }

        try {
            string json = File.ReadAllText(path);
            FloodSeasonConfig config = JsonUtility.FromJson<FloodSeasonConfig>(json);
            // Out-of-range values are clamped rather than rejected — a kid
            // can type "5" into the JSON and see five-times flow without
            // accidentally tanking the physics with a stray "10000".
            float clamped = Mathf.Clamp(config.Multiplier, MinMultiplier, MaxMultiplier);
            if (!Mathf.Approximately(clamped, config.Multiplier)) {
                Debug.LogWarning($"[Flood Season] multiplier {config.Multiplier} out of [{MinMultiplier}, {MaxMultiplier}]; clamped to {clamped}");
            }
            Multiplier = clamped;
            Debug.Log($"[Flood Season] loaded multiplier {Multiplier} from {path}");
        }
        catch (Exception e) {
            Debug.LogWarning($"[Flood Season] failed to read {path} ({e.Message}); using default multiplier {DefaultMultiplier}");
        }
    }

    // JsonUtility only deserialises [Serializable] classes with public
    // fields — not properties, not records. Mirror the shape of settings.json
    // exactly: one field per JSON key.
    [Serializable]
    private class FloodSeasonConfig {
        public float Multiplier = DefaultMultiplier;
    }

}
