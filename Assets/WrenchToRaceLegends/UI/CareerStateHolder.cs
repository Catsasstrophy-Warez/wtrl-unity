using UnityEngine;
using WTRL.Career;

namespace WTRL.UI
{
    /// <summary>
    /// Minimal scene-level owner of a single `CareerState` instance, so
    /// `GarageScreen`/`DynoScreen`/anything else can share one without a
    /// real save-loaded session existing yet (`WTRL.Persistence` can
    /// load/save a `CareerState`, but nothing in the Unity layer calls it
    /// -- this holder starts a fresh `CareerState()` with its defaults,
    /// same as `WTRL.Career.CareerState`'s own parameterless constructor
    /// gives every EditMode test). Wiring this to a real save file is
    /// future work, not this holder's job.
    /// </summary>
    public sealed class CareerStateHolder : MonoBehaviour
    {
        public CareerState State { get; private set; }

        private void Awake()
        {
            State = new CareerState();
        }
    }
}
