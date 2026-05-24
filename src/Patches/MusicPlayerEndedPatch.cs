using HarmonyLib;
using Timberborn.GameSound;
using Timberborn.HazardousWeatherSystem;

namespace Spycho.FloodSeason.Patches;

// Stops the "two music tracks playing on top of each other" symptom
// after loading a save that was mid-flood.
//
// Cause:
//
// FloodWeatherStatePersistence.PostLoad posts a fake
// HazardousWeatherEndedEvent(BadtideWeather) so that
// BadtideWaterSourceContaminationController disables itself — vanilla
// HazardousWeatherService.Load couldn't restore a flood (its boolean
// schema can't represent a third state), so on load it falls back to
// badtide, the contamination controller enables, and we have to undo
// that explicitly. See FloodWeatherStatePersistence for the full story.
//
// That fake EndedEvent also reaches GameMusicPlayer.OnHazardousWeatherEnded,
// which interprets it as "the hazardous phase ended, switch to standard
// music":
//
//     [OnEvent] public void OnHazardousWeatherEnded(...) {
//         StopDroughtMusic();
//         StartStandardMusic();
//     }
//
// But we ARE still mid-flood — the music player's own Load() ran
// earlier (Load phase precedes PostLoad), saw WeatherService.IsHazardous-
// Weather=true, and already called StartDroughtMusic. By the time our
// PostLoad fires, drought music is queued (with MinDelay), so StopSound
// doesn't cancel it cleanly, and StartStandardMusic schedules the
// standard track on top. Both play.
//
// Fix: prefix-skip the music player's handler when it sees the exact
// signature of our fake post: ended weather is BadtideWeather AND the
// current cycle's weather is FloodWeather. That combination is
// impossible in vanilla — when a real badtide ends, CurrentCycleHazardous-
// Weather is still BadtideWeather, not FloodWeather. So the prefix only
// suppresses our synthetic event, never a real one.
//
// FloodWeather.Instance is the static accessor set in FloodWeather's
// constructor; using it here avoids needing to field-inject Hazardous-
// WeatherService into the music player (which doesn't reference it).
[HarmonyPatch(typeof(GameMusicPlayer), nameof(GameMusicPlayer.OnHazardousWeatherEnded))]
internal static class MusicPlayerEndedPatch {

    [HarmonyPrefix]
    public static bool Prefix(HazardousWeatherEndedEvent hazardousWeatherEndedEvent) {
        if (hazardousWeatherEndedEvent.HazardousWeather is BadtideWeather
            && FloodWeather.Instance is { IsCurrent: true }) {
            // Our fake post — swallow it so the music keeps playing the
            // hazardous (a.k.a. drought) track that Load already started.
            return false;
        }
        return true;
    }

}
