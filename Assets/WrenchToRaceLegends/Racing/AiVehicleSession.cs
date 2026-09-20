using WTRL.Vehicle;

namespace WTRL.Racing
{
    /// <summary>
    /// New type, not a Swift port: the AI-driving analog of
    /// `WTRL.Runtime.WTRLRuntime`/`WTRL.UI.VehicleRuntimeController` --
    /// where those drive a player-input vehicle through
    /// `VehicleSimulation.Step` every frame, this drives an AI vehicle
    /// the same way, sourcing its input from `TrackAiDriver` instead of
    /// a keyboard. Closes the gap between this project's two previously
    /// separate proofs: `WTRL.UI.VehicleRuntimeControllerTests` (real
    /// physics, player input, no AI) and `SampleContentTests
    /// .TrackAiDriverCanFollowTheFoundryRowLineAllTheWayAround` (real AI
    /// perception/steering, but a simplified kinematic stand-in instead
    /// of real physics). This type runs the real
    /// `VehicleSimulation.Step` under AI control end to end.
    ///
    /// Deliberately has no content-catalog lookup, same discipline as
    /// every other WTRL assembly: every definition is a required
    /// constructor parameter, resolved by the caller.
    /// </summary>
    public sealed class AiVehicleSession
    {
        private readonly DriverModel _model;
        private readonly TrackLineDefinition _line;
        private readonly VehicleDefinition _vehicle;
        private readonly EngineDefinition _engine;
        private readonly TransmissionDefinition _transmission;
        private readonly TireDefinition _tire;
        private readonly SuspensionDefinition _suspension;
        private readonly VehicleTuning _tuning;
        private readonly string _rivalId;
        private int _lastNode;
        private ulong _tick;

        public VehicleSimState State;
        public DriverPerception? LastPerception { get; private set; }

        /// <summary>Optional -- when null, <see cref="Step"/> uses the
        /// plain <see cref="TrackAiDriver.Input(DriverModel,DriverPerception,double)"/>
        /// overload exactly as before. Set this (typically from a
        /// caller that owns a `RivalBehaviorRuntime`/career-level loss
        /// count -- `WTRL.Racing` deliberately has no dependency on
        /// `WTRL.Career`, so that computation is NOT this type's job,
        /// same "resolved by the caller" discipline as every other
        /// definition here) to make this session actually drive with
        /// intimidation: brake-point bias, defensive steering jitter,
        /// and pass-attempt suppression, exactly as `TrackAiDriver`'s
        /// intimidation-aware overload already implements and already
        /// has real tests for (`RivalIntimidationTests.cs`) -- that
        /// overload existed and was fully tested, but nothing in the
        /// actual AI-driving loop ever called it until now.</summary>
        public RivalIntimidationState? Intimidation { get; set; }

        /// <summary>Optional -- required only when <see cref="Intimidation"/>
        /// is set, since suppression/defensive behavior depends on
        /// where the player actually is. Left unset (default proximity,
        /// "not alongside") drives conservatively rather than throwing.</summary>
        public TrackAiDriver.PlayerProximity Proximity { get; set; }

        public AiVehicleSession(DriverModel model, TrackLineDefinition line, VehicleDefinition vehicle,
            EngineDefinition engine, TransmissionDefinition transmission, TireDefinition tire,
            SuspensionDefinition suspension, VehicleTuning? tuning = null, string rivalId = null)
        {
            _model = model;
            _line = line;
            _vehicle = vehicle;
            _engine = engine;
            _transmission = transmission;
            _tire = tire;
            _suspension = suspension;
            _tuning = tuning ?? VehicleTuning.Default;
            _rivalId = rivalId ?? vehicle.Id;

            State = VehicleSimState.Default();
            if (_line.Nodes.Count > 0)
            {
                State.X = _line.Nodes[0].X;
                State.Z = _line.Nodes[0].Z;
            }
        }

        /// <summary>Perceives the track line from the current position,
        /// derives AI input, and steps real vehicle physics by
        /// <paramref name="dt"/> seconds. A no-op (vehicle stays exactly
        /// where it is) if the track line has no nodes -- matches
        /// <see cref="TrackAiDriver.Perceive"/>'s own null-on-empty-line
        /// contract rather than silently substituting a default input.</summary>
        public void Step(double dt)
        {
            var perception = TrackAiDriver.Perceive(State, _line, _lastNode);
            if (perception == null) return;

            _lastNode = perception.Value.NodeIndex;
            LastPerception = perception;

            VehicleInput input;
            if (Intimidation.HasValue)
            {
                // Fixed-step-safe pseudo-randomness: the exact same
                // discipline the Swift original's 2026-09-19 fix
                // required (see TrackAiDriver's own doc comment) --
                // never call a platform RNG inside a deterministic sim
                // step, sample from RivalDeterministicSample keyed by
                // this session's own advancing tick instead.
                var defensiveSample = RivalDeterministicSample.Signed(_rivalId, _tick, "defense");
                var suppressionSample = RivalDeterministicSample.Unit(_rivalId, _tick, "suppression");
                input = TrackAiDriver.Input(_model, perception.Value, State.SpeedMps,
                    Intimidation.Value, Proximity, defensiveSample, suppressionSample);
            }
            else
            {
                input = TrackAiDriver.Input(_model, perception.Value, State.SpeedMps);
            }
            _tick++;

            VehicleSimulation.Step(ref State, input, _vehicle, _engine, _transmission, _tuning, _tire,
                _suspension, dt: dt);
        }
    }
}
