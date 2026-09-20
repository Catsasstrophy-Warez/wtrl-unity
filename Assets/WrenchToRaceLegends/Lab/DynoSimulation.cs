using System;
using System.Collections.Generic;
using WTRL.Garage;
using WTRL.Vehicle;

namespace WTRL.Lab
{
    /// <summary>
    /// Ported from SwiftRacer/Sources/WTRLCore/Simulation/DynoSimulation.swift.
    ///
    /// DEVIATION FROM THE SWIFT SOURCE (same shape as every other assembly
    /// so far): Swift's version resolved the base vehicle/transmission/
    /// suspension and its engine from a global `CanonicalContent` catalog
    /// by id. No such catalog exists here — the caller passes the already-
    /// resolved base <see cref="VehicleDefinition"/>/
    /// <see cref="TransmissionDefinition"/>/<see cref="EngineDefinition"/>
    /// directly. `VehicleConfigurationResolver.Resolve` itself never
    /// needed a catalog (see `WTRL.Garage/CONTRACT.md`), so only the engine
    /// lookup is a new required parameter here.
    /// </summary>
    public static class DynoSimulation
    {
        public static DynoRun Run(VehicleDefinition vehicle, TransmissionDefinition transmission,
            SuspensionDefinition? suspension, EngineDefinition engine, DynoConfiguration configuration,
            double duration = 8, double sampleHz = 20)
        {
            var resolved = VehicleConfigurationResolver.Resolve(vehicle, transmission, suspension, configuration.InstalledComponents);
            var normalizedFd = Math.Max(0, Math.Min(1, configuration.FinalDriveScale));
            var adjustedTransmission = resolved.Transmission with
            {
                FinalDrive = resolved.Transmission.FinalDrive * (0.90 + normalizedFd * 0.20),
            };
            var v = resolved.Vehicle;
            var t = adjustedTransmission;

            var gear = Math.Max(1, Math.Min(t.Ratios.Count,
                configuration.DynoGear ?? (t.Ratios.Count >= 4 ? 4 : t.Ratios.Count)));
            var ratio = t.Ratios[gear - 1] * t.FinalDrive;

            var samples = new List<DynoSample>();
            var peakP = 0.0;
            var peakT = 0.0;
            var count = Math.Max(2, (int)(duration * sampleHz));

            for (var i = 0; i < count; i++)
            {
                var f = (double)i / (count - 1);
                var rpm = engine.IdleRpm + (engine.RedlineRpm - engine.IdleRpm) * f;
                var nitrousFactor = 1.0 + Math.Max(0, Math.Min(1, configuration.NitrousNormalized)) * 0.08;
                var pressureFactor = 0.985 + (1 - Math.Abs(Math.Max(0, Math.Min(1, configuration.TirePressureNormalized)) - 0.45)) * 0.015;
                var torque = EngineSolver.TorqueNm(engine, rpm, throttle: 1, wear: 0) * nitrousFactor * pressureFactor * 0.86 * ratio;
                var wheelRpm = rpm / ratio;
                var speed = wheelRpm / 60 * 2 * Math.PI * v.TireRadiusM;
                var power = torque * (wheelRpm * 2 * Math.PI / 60) / 1000;
                peakP = Math.Max(peakP, power);
                peakT = Math.Max(peakT, torque);
                samples.Add(new DynoSample(i / sampleHz, rpm, speed, torque, power));
            }

            return new DynoRun($"dyno-{configuration.VehicleId}-baseline", configuration.VehicleId)
            {
                Samples = samples,
                PeakPowerKw = peakP,
                PeakTorqueNm = peakT,
            };
        }
    }
}
