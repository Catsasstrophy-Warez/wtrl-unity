using UnityEngine;
using WTRL.Runtime;

namespace WTRL.UI
{
    /// <summary>
    /// Wires `WTRL.Runtime.VehicleAudioState`'s 6 procedural audio layers
    /// (engine mechanical, intake, exhaust, driveline, tire, wind --
    /// computed every frame by `WTRLRuntime.Advance` but never consumed
    /// by anything) to 6 real Unity `AudioSource`s. Each layer's `Gain`
    /// maps to `AudioSource.volume`; `FrequencyHz` maps to `pitch` as a
    /// ratio against a reference frequency, since `AudioSource.pitch`
    /// scales playback speed rather than accepting an absolute
    /// frequency, and none of these layers have real recorded clips to
    /// pitch-shift yet (see "Not yet done" below).
    ///
    /// Requires a `VehicleRuntimeController` on the same GameObject (or
    /// assigned) to read `CurrentSnapshot` from every frame.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class VehicleAudioController : MonoBehaviour
    {
        [SerializeField] private VehicleRuntimeController vehicle;

        [Header("One AudioSource per procedural layer -- assign an engine-loop-style clip to each")]
        [SerializeField] private AudioSource engineMechanical;
        [SerializeField] private AudioSource intake;
        [SerializeField] private AudioSource exhaust;
        [SerializeField] private AudioSource driveline;
        [SerializeField] private AudioSource tire;
        [SerializeField] private AudioSource wind;

        /// <summary>The frequency each AudioSource's assigned clip is
        /// authored/recorded at -- pitch is set to
        /// `layer.FrequencyHz / referenceFrequencyHz` so a clip actually
        /// recorded near this frequency plays back close to natural
        /// speed at idle. Placeholder default (200Hz); tune per real
        /// recorded clip once one exists.</summary>
        [SerializeField] private float referenceFrequencyHz = 200f;

        private void Reset()
        {
            vehicle = GetComponent<VehicleRuntimeController>();
        }

        private void Update()
        {
            var snapshot = vehicle != null ? vehicle.CurrentSnapshot : null;
            if (snapshot?.Audio is not { } audio) return;

            Apply(engineMechanical, audio.EngineMechanical);
            Apply(intake, audio.Intake);
            Apply(exhaust, audio.Exhaust);
            Apply(driveline, audio.Driveline);
            Apply(tire, audio.Tire);
            Apply(wind, audio.Wind);
        }

        private void Apply(AudioSource source, AudioLayerState layer)
        {
            if (source == null) return;
            source.volume = Mathf.Clamp01((float)layer.Gain);
            source.pitch = Mathf.Max(0.01f, (float)layer.FrequencyHz / referenceFrequencyHz);
            if (layer.Gain > 0.001 && !source.isPlaying) source.Play();
            else if (layer.Gain <= 0.001 && source.isPlaying) source.Stop();
        }
    }
}
