using UnityEngine;
using WTRL.Career;
using WTRL.Persistence;

namespace WTRL.UI
{
    /// <summary>
    /// Scene-level owner of a single `CareerState` instance, now actually
    /// wired to `WTRL.Persistence.CareerSaveCodec` -- closes "the save/
    /// load round-trip exists but nothing in the Unity layer ever calls
    /// it" from the Milestone M8 gap audit. Loads a real save file on
    /// `Awake` (falling back to a fresh `CareerState()` if none exists
    /// yet, exactly like every EditMode test's default), and saves on
    /// `OnApplicationPause(true)` and `OnDestroy` -- the two real Unity
    /// lifecycle points this project's own Persistence CONTRACT.md
    /// already named as the missing hook, not an invented convention.
    ///
    /// HONEST LIMITATION: `CareerSaveCodec` explicitly does not persist
    /// `runEvidence`/`dynoRuns`/`ghostReplays` (its own doc comment,
    /// `WTRL.Persistence` has no `WTRL.Lab` dependency) -- that gap is
    /// unchanged by this wiring. Save path is
    /// `Application.persistentDataPath/career_save.json`, plain text
    /// JSON, no encryption or cloud-save adapter -- a real local save,
    /// not a production-hardened one.
    /// </summary>
    public sealed class CareerStateHolder : MonoBehaviour
    {
        public CareerState State { get; private set; }

        private static string SavePath =>
            System.IO.Path.Combine(Application.persistentDataPath, "career_save.json");

        private void Awake()
        {
            State = Load();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) Save();
        }

        private void OnDestroy()
        {
            Save();
        }

        /// <summary>Real, file-system-backed load -- separated from
        /// `Awake` so tests can exercise it without a live MonoBehaviour
        /// lifecycle.</summary>
        public static CareerState Load()
        {
            try
            {
                if (!System.IO.File.Exists(SavePath)) return new CareerState();
                var json = System.IO.File.ReadAllText(SavePath);
                return CareerSaveCodec.Decode(json);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"CareerStateHolder: failed to load save file, starting fresh. {ex.Message}");
                return new CareerState();
            }
        }

        /// <summary>Real, file-system-backed save of the current
        /// `State` -- separated from the lifecycle hooks so tests can
        /// exercise it directly.</summary>
        public void Save()
        {
            if (State == null) return;
            try
            {
                var json = CareerSaveCodec.Encode(State);
                System.IO.File.WriteAllText(SavePath, json);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"CareerStateHolder: failed to save. {ex.Message}");
            }
        }
    }
}
