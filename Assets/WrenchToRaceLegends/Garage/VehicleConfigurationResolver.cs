using System.Collections.Generic;
using WTRL.Vehicle;

namespace WTRL.Garage
{
    public sealed class ResolvedVehicleConfiguration
    {
        public VehicleDefinition Vehicle { get; }
        public TransmissionDefinition Transmission { get; }
        public SuspensionDefinition? Suspension { get; init; }

        // Constructor-enforced required fields, not the C# 11 `required`
        // keyword -- see WTRL.RPG/BuildRecipeProgress.cs's identical note
        // for why.
        public ResolvedVehicleConfiguration(VehicleDefinition vehicle, TransmissionDefinition transmission)
        {
            Vehicle = vehicle;
            Transmission = transmission;
        }
    }

    /// <summary>
    /// Ported from SwiftRacer/Sources/WTRLCore/Content/
    /// VehicleConfigurationResolver.swift. Applies a fixed, hardcoded set
    /// of part-ID effects on top of a base configuration — this is real
    /// but deliberately thin: the Swift original only ever recognized
    /// three part IDs (a final-drive swap and two grip/spring tweaks), and
    /// nothing since has extended it. Treat this as the seam where a real
    /// data-driven parts-effects system belongs, not as a finished
    /// workshop simulation.
    ///
    /// DEVIATION FROM THE SWIFT SOURCE (same shape as WTRL.Vehicle's):
    /// Swift's version looked up the base vehicle/transmission/suspension
    /// from a global `CanonicalContent` catalog by id. No such catalog
    /// exists here — the caller passes the already-resolved base
    /// definitions directly. This also required converting
    /// WTRL.Vehicle's definition types from plain classes to C# `record`s,
    /// so this resolver can derive a modified copy via `with` instead of
    /// mutating a shared instance in place (mutating a class instance
    /// in-place here would corrupt whatever else is holding a reference
    /// to that same canonical definition object).
    /// </summary>
    public static class VehicleConfigurationResolver
    {
        public static ResolvedVehicleConfiguration Resolve(VehicleDefinition vehicle,
            TransmissionDefinition transmission, SuspensionDefinition? suspension,
            IReadOnlyList<InstalledComponent> components)
        {
            foreach (var c in components)
            {
                switch (c.PartId)
                {
                    case "final-drive-373":
                        transmission = transmission with { FinalDrive = 3.73 };
                        break;
                    case "sport-tire":
                        vehicle = vehicle with { TireGripCoefficient = vehicle.TireGripCoefficient * 1.07 };
                        break;
                    case "track-damper":
                        if (suspension != null)
                        {
                            suspension = suspension with
                            {
                                FrontSpringRate = suspension.FrontSpringRate * 1.10,
                                RearSpringRate = suspension.RearSpringRate * 1.10,
                            };
                        }
                        break;
                }
            }

            return new ResolvedVehicleConfiguration(vehicle, transmission) { Suspension = suspension };
        }
    }
}
