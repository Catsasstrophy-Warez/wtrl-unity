using UnityEngine;
using WTRL.Career;
using WTRL.Garage;

namespace WTRL.UI
{
    /// <summary>
    /// A minimal, functional Garage screen -- closes the "build one real
    /// Garage UI screen" gap. Lists `WTRL.Garage.SampleContent
    /// .RecognizedParts` (the only 3 part ids `VehicleConfigurationResolver`
    /// actually does anything with), lets the player buy one (a real
    /// `CareerCommand.AcquirePart` + `Spend` batch, atomic) and install
    /// it into a fixed slot on the selected vehicle (a real
    /// `CareerCommand.Install`).
    ///
    /// Same IMGUI caveat as `TelemetryHud`: no Canvas/prefab UI exists
    /// yet, so this is `OnGUI`-based and meant to be replaced once
    /// someone can iterate on it visually. Functional, not designed.
    /// </summary>
    public sealed class GarageScreen : MonoBehaviour
    {
        [SerializeField] private CareerStateHolder careerState;
        [SerializeField] private bool visible = true;

        private string _statusMessage = "";

        private void OnGUI()
        {
            if (!visible || careerState == null || careerState.State == null) return;
            var state = careerState.State;

            GUILayout.BeginArea(new Rect(Screen.width - 340, 16, 320, 420), GUI.skin.box);
            GUILayout.Label($"Garage — {state.SelectedVehicleId}", HeaderStyle());
            GUILayout.Label($"Money: ${state.Money}");
            GUILayout.Space(8);

            foreach (var part in SampleContent.RecognizedParts)
            {
                GUILayout.BeginHorizontal();
                var owned = state.OwnedPartIds.Contains(part.Id);
                GUILayout.Label($"{part.Name} (${part.Price}){(owned ? " [owned]" : "")}");

                if (!owned && GUILayout.Button("Buy", GUILayout.Width(50)))
                {
                    var ok = CareerTransaction.Apply(new[]
                    {
                        CareerCommand.Spend(part.Price),
                        CareerCommand.AcquirePart(part.Id),
                    }, state);
                    _statusMessage = ok ? $"Bought {part.Name}." : "Not enough money.";
                }

                if (owned && GUILayout.Button("Install", GUILayout.Width(60)))
                {
                    var component = new InstalledComponent { PartId = part.Id, Slot = part.Category };
                    var ok = CareerTransaction.Apply(new[]
                    {
                        CareerCommand.Install(state.SelectedVehicleId, component),
                    }, state);
                    _statusMessage = ok ? $"Installed {part.Name}." : "Install failed.";
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(8);
            if (state.InstalledComponents.TryGetValue(state.SelectedVehicleId, out var installed))
            {
                GUILayout.Label("Installed:");
                foreach (var c in installed) GUILayout.Label($"  {c.Slot}: {c.PartId}");
            }

            if (!string.IsNullOrEmpty(_statusMessage))
            {
                GUILayout.Space(8);
                GUILayout.Label(_statusMessage);
            }
            GUILayout.EndArea();
        }

        private static GUIStyle HeaderStyle()
        {
            var style = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            return style;
        }
    }
}
