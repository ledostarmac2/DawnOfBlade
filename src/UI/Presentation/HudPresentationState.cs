using DawnOfBlade.World.Grid;

namespace DawnOfBlade.UI.Presentation;

/// <summary>Client-side HUD state derived from authoritative snapshots and local tab selection.</summary>
public sealed class HudPresentationState
{
    public GridCoordinate VerifiedTile { get; private set; }
    public HudTab ActiveTab { get; private set; } = HudTab.Inventory;
    public string CoordinateText => $"Coord: X: {VerifiedTile.X:N0} | Z: {VerifiedTile.Z:N0}";

    public void ApplyVerifiedTile(GridCoordinate tile) => VerifiedTile = tile;

    public void SelectTab(HudTab tab) => ActiveTab = tab;
}
