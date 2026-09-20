using System;
using System.Linq;
using WTRL.Vehicle;

namespace WTRL.Runtime
{
    // Ported from SwiftRacer/Sources/WTRLCore/Runtime/PresentationState.swift.

    public struct VehiclePresentationState
    {
        public double[] WheelAngularVelocity;
        public double[] TireSlip;
        public double[] SuspensionTravel;
        public double EngineRpm;
        public double EngineLoad;
        public double RoadSpeedMps;
        public double PitchRadians;
        public double RollRadians;
        public double SkidIntensity;
        public double SmokeIntensity;

        public VehiclePresentationState(VehicleSimState sim, VehicleInput input)
        {
            WheelAngularVelocity = sim.Wheels.Select(w => w.AngularVelocityRadS).ToArray();
            TireSlip = sim.Tires.Select(t => Math.Sqrt(t.SlipRatio * t.SlipRatio + t.SlipAngle * t.SlipAngle)).ToArray();
            SuspensionTravel = sim.SuspensionCorners.Select(c => c.DisplacementM).ToArray();
            EngineRpm = sim.EngineRpm;
            EngineLoad = Math.Max(0, input.Throttle);
            RoadSpeedMps = sim.SpeedMps;
            PitchRadians = sim.Suspension.PitchRadians;
            RollRadians = sim.Suspension.RollRadians;
            var avgSlip = TireSlip.Length > 0 ? TireSlip.Sum() / TireSlip.Length : 0;
            SkidIntensity = Math.Max(0, Math.Min(1, (avgSlip - 0.08) / 0.55));
            SmokeIntensity = Math.Max(0, Math.Min(1, (avgSlip - 0.25) / 0.85)) * Math.Min(1, Math.Abs(sim.SpeedMps) / 8);
        }
    }

    public struct AudioLayerState
    {
        public double FrequencyHz;
        public double Gain;
    }

    public struct VehicleAudioState
    {
        public double Rpm;
        public double Load;
        public double AverageTireSlip;
        public double DrivelineSpeedRpm;
        public AudioLayerState EngineMechanical;
        public AudioLayerState Intake;
        public AudioLayerState Exhaust;
        public AudioLayerState Driveline;
        public AudioLayerState Tire;
        public AudioLayerState Wind;

        public VehicleAudioState(VehiclePresentationState presentation, double drivelineSpeedRpm)
        {
            Rpm = presentation.EngineRpm;
            Load = presentation.EngineLoad;
            AverageTireSlip = presentation.TireSlip.Length > 0 ? presentation.TireSlip.Sum() / presentation.TireSlip.Length : 0;
            DrivelineSpeedRpm = drivelineSpeedRpm;
            var firingBase = Math.Max(25, Rpm / 60 * 4);
            EngineMechanical = new AudioLayerState { FrequencyHz = firingBase * 0.5, Gain = Math.Min(1, 0.22 + Load * 0.45) };
            Intake = new AudioLayerState { FrequencyHz = firingBase, Gain = Math.Min(1, 0.08 + Load * 0.72) };
            Exhaust = new AudioLayerState { FrequencyHz = firingBase * 0.75, Gain = Math.Min(1, 0.18 + Load * 0.82) };
            Driveline = new AudioLayerState { FrequencyHz = Math.Max(30, drivelineSpeedRpm / 60 * 3), Gain = Math.Min(0.75, Math.Abs(drivelineSpeedRpm) / 9000) };
            Tire = new AudioLayerState { FrequencyHz = 350 + AverageTireSlip * 900, Gain = Math.Min(1, AverageTireSlip * 1.8) };
            Wind = new AudioLayerState { FrequencyHz = 180 + presentation.RoadSpeedMps * 8, Gain = Math.Min(1, Math.Abs(presentation.RoadSpeedMps) / 55) };
        }
    }
}
