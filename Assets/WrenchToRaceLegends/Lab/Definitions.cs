using System.Collections.Generic;
using WTRL.Garage;

namespace WTRL.Lab
{
    // Ported from SwiftRacer/Sources/WTRLCore/Content/Definitions.swift
    // (DynoSample, DynoRun) and Runtime/RunEvidence.swift (DynoConfiguration).

    public readonly struct DynoSample
    {
        public readonly double Time;
        public readonly double Rpm;
        public readonly double SpeedMps;
        public readonly double WheelTorqueNm;
        public readonly double WheelPowerKw;

        public DynoSample(double time, double rpm, double speedMps, double wheelTorqueNm, double wheelPowerKw)
        {
            Time = time;
            Rpm = rpm;
            SpeedMps = speedMps;
            WheelTorqueNm = wheelTorqueNm;
            WheelPowerKw = wheelPowerKw;
        }
    }

    public sealed class DynoRun
    {
        public required string Id { get; init; }
        public required string VehicleId { get; init; }
        public IReadOnlyList<DynoSample> Samples { get; init; } = System.Array.Empty<DynoSample>();
        public double PeakPowerKw { get; init; }
        public double PeakTorqueNm { get; init; }
    }

    public sealed class DynoConfiguration
    {
        public string VehicleId { get; init; }
        public IReadOnlyList<InstalledComponent> InstalledComponents { get; init; } = System.Array.Empty<InstalledComponent>();
        public double FinalDriveScale { get; init; } = 0.5;
        public double TirePressureNormalized { get; init; } = 0.5;
        public double NitrousNormalized { get; init; } = 0.0;
        public int? DynoGear { get; init; }

        public DynoConfiguration(string vehicleId)
        {
            VehicleId = vehicleId;
        }
    }
}
