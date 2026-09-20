using NUnit.Framework;
using UnityEngine;
using WTRL.UI;

namespace WTRL.Tests
{
    /// <summary>
    /// Exercises the REAL save/load wiring added to `CareerStateHolder`
    /// -- closes "the save/load round-trip exists but nothing in the
    /// Unity layer ever calls it". Writes and reads an actual file under
    /// `Application.persistentDataPath` (this test's own machine/CI temp
    /// profile directory, not a mock filesystem), and cleans it up
    /// afterward so repeated runs don't see stale state.
    /// </summary>
    public class CareerStateHolderPersistenceTests
    {
        private static string SavePath =>
            System.IO.Path.Combine(Application.persistentDataPath, "career_save.json");

        [SetUp]
        public void DeleteAnyExistingSaveFile()
        {
            if (System.IO.File.Exists(SavePath)) System.IO.File.Delete(SavePath);
        }

        [TearDown]
        public void CleanUpSaveFile()
        {
            if (System.IO.File.Exists(SavePath)) System.IO.File.Delete(SavePath);
        }

        [Test]
        public void LoadWithNoExistingSaveFileReturnsAFreshCareerState()
        {
            var state = CareerStateHolder.Load();
            Assert.That(state.Money, Is.EqualTo(5000)); // CareerState's own default starting money
            Assert.That(state.CompletedRaceIds, Is.Empty);
        }

        [Test]
        public void SaveThenLoadRoundTripsRealMoneyAndCompletedRaces()
        {
            var go = new GameObject("CareerStateHolder");
            var holder = go.AddComponent<CareerStateHolder>(); // Awake() loads a fresh state
            holder.State.Money = 4200;
            holder.State.Reputation = 15;
            holder.State.CompletedRaceIds.Add("club-circuit-01");

            holder.Save();
            Assert.That(System.IO.File.Exists(SavePath), Is.True, "Save() should have written a real file to disk");

            var reloaded = CareerStateHolder.Load();
            Assert.That(reloaded.Money, Is.EqualTo(4200));
            Assert.That(reloaded.Reputation, Is.EqualTo(15));
            Assert.That(reloaded.CompletedRaceIds, Does.Contain("club-circuit-01"));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDestroyLifecycleHookActuallyPersistsState()
        {
            var go = new GameObject("CareerStateHolder");
            var holder = go.AddComponent<CareerStateHolder>();
            holder.State.Money = 777;

            // OnDestroy is the real lifecycle hook this wiring uses to
            // save on scene teardown/app quit -- DestroyImmediate fires
            // it synchronously in the Editor, so this actually exercises
            // that code path rather than calling Save() directly.
            Object.DestroyImmediate(go);

            var reloaded = CareerStateHolder.Load();
            Assert.That(reloaded.Money, Is.EqualTo(777));
        }
    }
}
