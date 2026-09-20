using UnityEngine;
using WTRL.Content;
using WTRL.Racing;
using WTRL.Vehicle;

namespace WTRL.UI
{
    /// <summary>
    /// MonoBehaviour wrapper around `WTRL.Racing.AiVehicleSession` --
    /// the visual/scene counterpart to `VehicleRuntimeController`, but
    /// driven by `TrackAiDriver` instead of player input. Lets an AI
    /// vehicle (e.g. a rival) actually appear and move in a scene, not
    /// just in a test's assertions.
    /// </summary>
    public sealed class AiVehicleController : MonoBehaviour
    {
        [Header("Required content (no fallback -- all must be assigned)")]
        public VehicleDefinitionAsset vehicle;

        [Header("AI")]
        public DriverAggressionProfile driverProfile = DriverAggressionProfile.Balanced;

        private AiVehicleSession _session;

        public enum DriverAggressionProfile { Cautious, Balanced, Aggressive }

        private void Awake()
        {
            if (vehicle == null || vehicle.engine == null || vehicle.transmission == null ||
                vehicle.suspension == null || vehicle.tire == null)
            {
                Debug.LogError($"{nameof(AiVehicleController)} requires a fully-assigned " +
                    $"{nameof(VehicleDefinitionAsset)} (vehicle + engine + transmission + suspension + tire).");
                enabled = false;
                return;
            }

            var model = driverProfile switch
            {
                DriverAggressionProfile.Cautious => new DriverModel
                {
                    Aggression = 0.3, Consistency = 0.9, BrakingConfidence = 0.6, ThrottleDiscipline = 0.9,
                    WetSkill = 0.7, TireConservation = 0.8, MechanicalSympathy = 0.8, MistakeProbability = 0.01,
                },
                DriverAggressionProfile.Aggressive => new DriverModel
                {
                    Aggression = 0.85, Consistency = 0.6, BrakingConfidence = 0.85, ThrottleDiscipline = 0.6,
                    WetSkill = 0.5, TireConservation = 0.3, MechanicalSympathy = 0.3, MistakeProbability = 0.05,
                },
                _ => new DriverModel
                {
                    Aggression = 0.55, Consistency = 0.75, BrakingConfidence = 0.7, ThrottleDiscipline = 0.75,
                    WetSkill = 0.6, TireConservation = 0.55, MechanicalSympathy = 0.55, MistakeProbability = 0.02,
                },
            };

            _session = new AiVehicleSession(model, SampleContent.FoundryRowCircuitLine(), vehicle.ToDefinition(),
                vehicle.engine.ToDefinition(), vehicle.transmission.ToDefinition(), vehicle.tire.ToDefinition(),
                vehicle.suspension.ToDefinition());
        }

        private void FixedUpdate()
        {
            if (_session == null) return;
            _session.Step(Time.fixedDeltaTime);

            var sim = _session.State;
            transform.SetPositionAndRotation(
                new Vector3((float)sim.X, transform.position.y, (float)sim.Z),
                Quaternion.Euler(0, (float)(sim.HeadingRadians * Mathf.Rad2Deg), 0));
        }
    }
}
