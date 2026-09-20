using UnityEngine;
using WTRL.Vehicle;

namespace WTRL.Content
{
    /// <summary>See EngineDefinitionAsset.cs's doc comment: one
    /// ScriptableObject type per file, to avoid a real Unity Editor
    /// batchmode script-reference bug.</summary>
    [CreateAssetMenu(fileName = "SurfaceDefinition", menuName = "WTRL/Content/Surface Definition")]
    public sealed class SurfaceDefinitionAsset : ScriptableObject
    {
        public string id;
        public SurfaceKind kind;
        public double dryGripMultiplier = 1.0;
        public double rollingResistanceMultiplier = 1.0;
        public double roughness = 0.2;

        public SurfaceDefinition ToDefinition() => new(id, kind, dryGripMultiplier)
        {
            RollingResistanceMultiplier = rollingResistanceMultiplier,
            Roughness = roughness,
        };
    }
}
