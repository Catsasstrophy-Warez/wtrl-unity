using UnityEngine;
using WTRL.World;

namespace WTRL.UI
{
    /// <summary>
    /// Unity-side wiring for `WTRL.World.WorldStreamingGrid`, which was
    /// deliberately built presentation-agnostic (see `World/CONTRACT.md`)
    /// and had nothing consuming its load/unload deltas anywhere in the
    /// project until now. Tracks a follow target's position, calls
    /// `WorldStreamingGrid.Update` once per frame, and raises
    /// <see cref="CellsChanged"/> with the resulting deltas.
    ///
    /// Deliberately does NOT instantiate/destroy any GameObjects itself
    /// -- there is no cell-content prefab authoring anywhere in this
    /// project yet (see `World/CONTRACT.md`'s "actual cell-content
    /// authoring" note), so this component only exposes the real
    /// load/unload signal for whatever eventually does that job to
    /// subscribe to. Logs cell changes via <see cref="Debug.Log"/> so the
    /// signal is at least observable in the Console before that consumer
    /// exists.
    /// </summary>
    public sealed class WorldStreamingController : MonoBehaviour
    {
        [SerializeField] private Transform followTarget;
        [SerializeField] private float cellSizeMeters = 160;
        [SerializeField] private int activeRadiusCells = 2;
        [SerializeField] private bool logCellChanges = true;

        private WorldStreamingGrid _grid;

        public delegate void CellsChangedHandler(System.Collections.Generic.IReadOnlyList<WorldCellId> loaded,
            System.Collections.Generic.IReadOnlyList<WorldCellId> unloaded);

        public event CellsChangedHandler? CellsChanged;

        private void Awake()
        {
            _grid = new WorldStreamingGrid(cellSizeMeters, activeRadiusCells);
        }

        private void Update()
        {
            if (followTarget == null) return;

            var (toLoad, toUnload) = _grid.Update(followTarget.position.x, followTarget.position.z);
            if (toLoad.Count == 0 && toUnload.Count == 0) return;

            if (logCellChanges)
            {
                Debug.Log($"WorldStreamingController: +{toLoad.Count} cells, -{toUnload.Count} cells " +
                    $"(active radius {activeRadiusCells}, cell size {cellSizeMeters}m)");
            }
            CellsChanged?.Invoke(toLoad, toUnload);
        }
    }
}
