using System.Linq;
using NUnit.Framework;
using WTRL.Events;
using WTRL.World;

namespace WTRL.Tests
{
    /// <summary>Ported from WTRLCoreTests.swift's testRaceRuntimeFinishesAtLapCount
    /// and WTRLAdvancedTests.swift's testRaceCountdownStartsRace/
    /// testRaceFalseStartAddsPenalty/testRaceLapTimeRecorded/
    /// testDragSplitCapture/testDragQuarterMileFinishes, against a locally
    /// built RaceDefinition rather than the real "club-circuit-01" catalog
    /// entry (same reasoning as every prior assembly's ported tests — no
    /// content catalog exists here). Plus new tests for WorldStreamingGrid,
    /// which has no direct Swift-test equivalent (it was never wired into
    /// anything in SwiftRacer). Run via the same throwaway dotnet test
    /// project as every other assembly — all pass.</summary>
    public class WorldAndEventsTests
    {
        private static RaceDefinition MakeRace(int laps = 3) =>
            new RaceDefinition("test-race", "Test Circuit", "test-track", laps, reputationRequired: 0);

        [Test]
        public void RaceRuntimeFinishesAtLapCount()
        {
            var s = new RaceRuntimeState("test-race");
            var race = MakeRace(laps: 3);
            RaceRules.Start(s);
            for (var i = 0; i < race.Laps; i++) RaceRules.CompleteLap(s, race);
            Assert.That(s.Phase, Is.EqualTo(RacePhase.Finished));
        }

        [Test]
        public void RaceCountdownStartsRace()
        {
            var s = new RaceRuntimeState("test-race");
            RaceRules.BeginCountdown(s, seconds: 0.1);
            RaceRules.Advance(s, dt: 0.2);
            Assert.That(s.Phase, Is.EqualTo(RacePhase.Running));
        }

        [Test]
        public void RaceFalseStartAddsPenalty()
        {
            var s = new RaceRuntimeState("test-race");
            RaceRules.RegisterFalseStart(s);
            Assert.That(s.FalseStart, Is.True);
            Assert.That(s.ClassifiedTime, Is.GreaterThan(s.Elapsed));
        }

        [Test]
        public void RaceLapTimeRecorded()
        {
            var s = new RaceRuntimeState("test-race");
            var race = MakeRace();
            RaceRules.Start(s);
            RaceRules.Advance(s, dt: 12);
            RaceRules.CompleteLap(s, race);
            Assert.That(s.LapTimes.First(), Is.EqualTo(12));
        }

        [Test]
        public void DragSplitCapture()
        {
            var d = new DragRaceRuntimeState();
            DragRaceRules.Stage(d);
            DragRaceRules.Green(d, reactionTime: 0.2);
            for (var i = 0; i < 1000; i++) DragRaceRules.Advance(d, speedMps: 60, dt: 1.0 / 120);
            Assert.That(d.Splits, Is.Not.Empty);
        }

        [Test]
        public void DragQuarterMileFinishes()
        {
            var d = new DragRaceRuntimeState();
            DragRaceRules.Stage(d);
            DragRaceRules.Green(d, reactionTime: 0.1);
            for (var i = 0; i < 1200; i++) DragRaceRules.Advance(d, speedMps: 60, dt: 1.0 / 120);
            Assert.That(d.Finished, Is.True);
        }

        [Test]
        public void CompleteLapDoesNothingBeforeRaceStarts()
        {
            // RaceRules guards every mutator on the current phase -- a lap
            // can't complete on a still-Staged race.
            var s = new RaceRuntimeState("test-race");
            RaceRules.CompleteLap(s, MakeRace());
            Assert.That(s.Lap, Is.EqualTo(0));
            Assert.That(s.Phase, Is.EqualTo(RacePhase.Staged));
        }

        [Test]
        public void StableWorldSeedIsDeterministic()
        {
            var id = new WorldCellId(3, -7);
            Assert.That(StableWorldSeed.Value(id, 42), Is.EqualTo(StableWorldSeed.Value(id, 42)));
            Assert.That(StableWorldSeed.Value(id, 42), Is.Not.EqualTo(StableWorldSeed.Value(id, 43)));
        }

        [Test]
        public void WorldStreamingGridLoadsSurroundingCellsOnFirstUpdate()
        {
            var grid = new WorldStreamingGrid(cellSize: 160, activeRadius: 1);
            var (toLoad, toUnload) = grid.Update(playerX: 0, playerZ: 0);
            // A radius-1 grid around the origin cell is a 3x3 block.
            Assert.That(toLoad.Count, Is.EqualTo(9));
            Assert.That(toUnload, Is.Empty);
            Assert.That(grid.LoadedCells.Count, Is.EqualTo(9));
        }

        [Test]
        public void WorldStreamingGridUnloadsCellsLeftBehindAfterMoving()
        {
            var grid = new WorldStreamingGrid(cellSize: 160, activeRadius: 1);
            grid.Update(playerX: 0, playerZ: 0);
            var (toLoad, toUnload) = grid.Update(playerX: 2000, playerZ: 0); // far enough to shift the whole window
            Assert.That(toUnload, Is.Not.Empty);
            Assert.That(toLoad, Is.Not.Empty);
        }

        [Test]
        public void WorldStreamingGridIsIdempotentWithoutMovement()
        {
            var grid = new WorldStreamingGrid(cellSize: 160, activeRadius: 1);
            grid.Update(playerX: 0, playerZ: 0);
            var (toLoad, toUnload) = grid.Update(playerX: 1, playerZ: 1); // still inside the same cell
            Assert.That(toLoad, Is.Empty);
            Assert.That(toUnload, Is.Empty);
        }

        [Test]
        public void RaceFlowControllerProgressesThroughAllEightStatesInOrder()
        {
            var race = new RaceDefinition("club-circuit-01", "Club Circuit", "foundry-row-circuit", laps: 1,
                reputationRequired: 0);
            var flow = new RaceFlowController(race);

            Assert.That(flow.Phase, Is.EqualTo(RaceFlowPhase.Inactive));
            flow.BeginLoading();
            Assert.That(flow.Phase, Is.EqualTo(RaceFlowPhase.Loading));
            flow.FinishLoading();
            Assert.That(flow.Phase, Is.EqualTo(RaceFlowPhase.Staging));
            flow.BeginCountdown(1);
            Assert.That(flow.Phase, Is.EqualTo(RaceFlowPhase.Countdown));

            flow.Advance(1); // exhausts countdown, auto-starts the inner state machine
            Assert.That(flow.Phase, Is.EqualTo(RaceFlowPhase.Racing));

            flow.Advance(30);
            flow.CompleteLap(); // laps: 1 -> inner state machine finishes
            Assert.That(flow.Phase, Is.EqualTo(RaceFlowPhase.Finishing));

            flow.ShowResults();
            Assert.That(flow.Phase, Is.EqualTo(RaceFlowPhase.Results));
            flow.Complete();
            Assert.That(flow.Phase, Is.EqualTo(RaceFlowPhase.Complete));
        }

        [Test]
        public void RaceFlowControllerIgnoresOutOfOrderTransitions()
        {
            var race = new RaceDefinition("club-circuit-01", "Club Circuit", "foundry-row-circuit", laps: 1,
                reputationRequired: 0);
            var flow = new RaceFlowController(race);

            flow.BeginCountdown(3); // illegal from Inactive -- should be a no-op
            Assert.That(flow.Phase, Is.EqualTo(RaceFlowPhase.Inactive));

            flow.Complete(); // illegal from Inactive -- should be a no-op
            Assert.That(flow.Phase, Is.EqualTo(RaceFlowPhase.Inactive));
        }
    }
}
