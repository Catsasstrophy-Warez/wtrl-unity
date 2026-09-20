using System;
using System.Collections.Generic;
using WTRL.Lab;
using WTRL.Vehicle;

namespace WTRL.Runtime
{
    public struct SamplingPolicy
    {
        public double PhysicsHz;
        public double TelemetryHz;
        public double NumericUiHz;
        public double DiagnosticsHz;

        public static SamplingPolicy Default => new SamplingPolicy
        {
            PhysicsHz = 120,
            TelemetryHz = 30,
            NumericUiHz = 15,
            DiagnosticsHz = 5,
        };
    }

    public struct WTRLSnapshot
    {
        public VehicleSimState Vehicle;
        public double ElapsedSeconds;
        public string VehicleId;
        public string SurfaceId;
        public List<DiagnosticFinding> Diagnostics;
        public VehiclePresentationState? Presentation;
        public VehicleAudioState? Audio;

        public static WTRLSnapshot Create(string vehicleId = "hero-1965") => new WTRLSnapshot
        {
            Vehicle = VehicleSimState.Default(),
            VehicleId = vehicleId,
            SurfaceId = "asphalt",
            Diagnostics = new List<DiagnosticFinding>(),
        };
    }

    /// <summary>
    /// Ported from SwiftRacer/Sources/WTRLCore/Runtime/WTRLRuntime.swift —
    /// the composition root that drives <see cref="VehicleSimulation.Step"/>
    /// through a <see cref="FixedStepClock"/>, computes presentation/audio
    /// state, and records rate-gated telemetry every frame.
    ///
    /// DEVIATION (the same "no content catalog" pattern every prior
    /// assembly established): Swift's <c>advance(frameDelta:input:)</c>
    /// resolved the vehicle's configuration internally every frame via
    /// <c>VehicleConfigurationResolver.resolve(vehicleID:...)</c> and
    /// <c>CanonicalContent.engine/surface(...)</c> lookups. No catalog
    /// exists anywhere in this project (see `WTRL.Vehicle/CONTRACT.md`'s
    /// deviation #1, and every assembly ported since). This port's
    /// <see cref="Advance"/> therefore takes the already-resolved
    /// <see cref="VehicleDefinition"/>/<see cref="EngineDefinition"/>/
    /// <see cref="TransmissionDefinition"/>/<see cref="TireDefinition"/>/
    /// <see cref="SuspensionDefinition"/>?/<see cref="SurfaceDefinition"/>?
    /// directly, every call — matching `VehicleSimulation.Step`'s own
    /// signature exactly. Whatever eventually owns content resolution
    /// (still an open architecture question across this whole project)
    /// is responsible for calling `WTRL.Garage.VehicleConfigurationResolver
    /// .Resolve` once per frame (or caching it when nothing installed has
    /// changed) and passing the result in here.
    /// </summary>
    public sealed class WTRLRuntime
    {
        private readonly object _lock = new();
        private WTRLSnapshot _state;

        // NOTE: `new FixedStepClock()` (empty parens) does NOT call the
        // constructor overload `FixedStepClock(double hz = 120)` despite
        // its default parameter -- for structs, a zero-argument `new T()`
        // always binds to the implicit zero-initializer, never a
        // user-defined constructor, even one whose only parameter is
        // optional. That silently produced `Step == 0`, which turns
        // `Consume`'s `while (_accumulator >= Step)` into an infinite
        // loop (caught by a real `dotnet test` hang during this port's
        // own verification, not by inspection). Always pass the rate
        // explicitly.
        private FixedStepClock _clock = new(SamplingPolicy.Default.PhysicsHz);
        private readonly RuntimeTelemetryRing _telemetry = new();
        private double _nextTelemetrySampleTime;

        public SamplingPolicy SamplingPolicy { get; set; } = SamplingPolicy.Default;
        public VehicleTuning Tuning { get; set; } = VehicleTuning.Default;

        /// <summary>Set by whatever owns track selection
        /// (<c>WTRL.World.TrackDefinition.BankingDegrees</c>) whenever the
        /// active track changes. Defaults to 0 (flat).</summary>
        public double BankingDegrees { get; set; }

        public WTRLRuntime(string vehicleId = "hero-1965")
        {
            _state = WTRLSnapshot.Create(vehicleId);
        }

        public WTRLSnapshot Snapshot
        {
            get { lock (_lock) return _state; }
        }

        public IReadOnlyList<RuntimeTelemetrySample> TelemetrySamples
        {
            get { lock (_lock) return _telemetry.Samples; }
        }

        public void Reset(string? vehicleId = null, double x = 0, double z = 0, double heading = 0)
        {
            lock (_lock)
            {
                var id = vehicleId ?? _state.VehicleId;
                var surface = _state.SurfaceId;
                _state = WTRLSnapshot.Create(id);
                _state.SurfaceId = surface;
                _state.Vehicle.X = x;
                _state.Vehicle.Z = z;
                _state.Vehicle.HeadingRadians = heading;
                _clock = new FixedStepClock(SamplingPolicy.PhysicsHz);
                _nextTelemetrySampleTime = 0;
            }
        }

        public void SetSurface(string surfaceId)
        {
            lock (_lock) _state.SurfaceId = surfaceId;
        }

        public void SetShiftMode(ShiftMode mode)
        {
            lock (_lock) _state.Vehicle.Drivetrain.ShiftMode = mode;
        }

        /// <summary><paramref name="suspension"/> is required, not
        /// nullable — matching <see cref="VehicleSimulation.Step"/>'s own
        /// signature exactly. That parameter was made required
        /// specifically so this project never silently fabricates a
        /// fallback suspension the way the Swift original's global-catalog
        /// lookup could; this method must not reintroduce that by
        /// defaulting to a placeholder when the caller passes
        /// none.</summary>
        public void Advance(double frameDelta, VehicleInput input, VehicleDefinition vehicle, EngineDefinition engine,
            TransmissionDefinition transmission, TireDefinition tire, SuspensionDefinition suspension,
            SurfaceDefinition? surface)
        {
            lock (_lock)
            {
                var localVehicle = _state.Vehicle;
                var count = _clock.Consume(frameDelta, dt =>
                {
                    VehicleSimulation.Step(ref localVehicle, input, vehicle, engine, transmission, Tuning, tire,
                        suspension, surface, BankingDegrees, default, dt);
                    _state.ElapsedSeconds += dt;
                });
                _state.Vehicle = localVehicle;

                _state.Diagnostics = VehicleSimulation.Diagnose(_state.Vehicle);
                var presentation = new VehiclePresentationState(_state.Vehicle, input);
                _state.Presentation = presentation;
                _state.Audio = new VehicleAudioState(presentation, _state.Vehicle.Drivetrain.OutputShaftRpm);

                var avgSlip = presentation.TireSlip.Length > 0 ? System.Linq.Enumerable.Sum(presentation.TireSlip) / presentation.TireSlip.Length : 0;

                // Rate-gated to SamplingPolicy.TelemetryHz (default 30)
                // rather than every 120Hz physics step — see the Swift
                // source's identical comment: at the physics rate a
                // 600-capacity ring buffer fills in 5 seconds of sim
                // time; at 30Hz it covers 20, a far more useful window
                // for HUD/debug consumers. The very first call always
                // samples (nextTelemetrySampleTime starts at 0).
                if (_state.ElapsedSeconds >= _nextTelemetrySampleTime)
                {
                    var engineTorque = EngineSolver.TorqueNm(engine, _state.Vehicle.EngineRpm, input.Throttle, _state.Vehicle.Damage.EngineWear);
                    _telemetry.Append(new RuntimeTelemetrySample
                    {
                        SimulationSeconds = _state.ElapsedSeconds,
                        WallFrameDelta = frameDelta,
                        FixedSteps = count,
                        SpeedMps = _state.Vehicle.SpeedMps,
                        EngineRpm = _state.Vehicle.EngineRpm,
                        Gear = _state.Vehicle.Gear,
                        AverageTireSlip = avgSlip,
                        LongitudinalAcceleration = _state.Vehicle.LongitudinalAcceleration,
                        LateralAcceleration = _state.Vehicle.LateralAcceleration,
                        EngineTorqueNm = engineTorque,
                        WheelTorqueNm = _state.Vehicle.Drivetrain.LeftDriveTorqueNm + _state.Vehicle.Drivetrain.RightDriveTorqueNm,
                        ClutchEngagement = _state.Vehicle.Drivetrain.ClutchEngagement,
                        FrontBrakeTemperatureC = _state.Vehicle.Brakes.FrontTemperatureC,
                        RearBrakeTemperatureC = _state.Vehicle.Brakes.RearTemperatureC,
                        TireTemperaturesC = Array.ConvertAll(_state.Vehicle.Tires, t => t.TemperatureC),
                        TireNormalLoadsN = Array.ConvertAll(_state.Vehicle.Tires, t => t.NormalLoadN),
                        SuspensionTravelM = Array.ConvertAll(_state.Vehicle.SuspensionCorners, c => c.DisplacementM),
                    });
                    _nextTelemetrySampleTime = _state.ElapsedSeconds + 1 / Math.Max(1, SamplingPolicy.TelemetryHz);
                }
            }
        }
    }
}
