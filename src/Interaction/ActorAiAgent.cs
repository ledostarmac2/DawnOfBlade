using DawnOfBlade.Engine.Ai;
using DawnOfBlade.Engine.Spatial;
using Godot;

namespace DawnOfBlade.Interaction;

/// <summary>
/// Runtime glue between an engine-pure <see cref="ActorBrain"/> and a visual <see cref="Node3D"/>.
/// Each heartbeat the scene calls <see cref="Tick"/>: the brain advances one tile of behavior and
/// this agent maps the brain's tile back to a world position on the body. The mapping mirrors the
/// game's tile convention — tile X maps to world X, tile Y maps to world Z — anchored so the body's
/// starting placement corresponds to the brain's spawn anchor.
/// <para>
/// The brain moves in whole-tile steps; a visual layer is free to interpolate the body toward the
/// agent's reported position between ticks. All decision logic lives in the brain (unit-tested in
/// <c>tests/AiTests.cs</c>); this class is only the coordinate bridge (engine-tested in
/// <c>test/HeadlessTests.tscn</c>).
/// </para>
/// </summary>
public partial class ActorAiAgent : Node
{
    private Node3D _body = null!;
    private ActorBrain _brain = null!;
    private CollisionGrid _grid = null!;
    private TrueTile _origin;
    private Vector3 _originWorld;
    private float _tileSize = 1.0f;
    private bool _configured;

    public BrainStep LastStep { get; private set; }

    public TrueTile Tile => _brain.Position;

    public AiState State => _brain.State;

    /// <summary>Wires the agent to a body and brain. Captures the body's current world position as
    /// the world location of the brain's spawn anchor.</summary>
    public void Configure(Node3D body, ActorBrain brain, CollisionGrid grid, float tileSizeMeters)
    {
        _body = body;
        _brain = brain;
        _grid = grid;
        _tileSize = tileSizeMeters;
        _origin = brain.Anchor;
        _originWorld = body.GlobalPosition;
        _configured = true;
    }

    /// <summary>Advances one tick of AI and snaps the body to the brain's resulting tile.</summary>
    public BrainStep Tick(in Perception perception)
    {
        if (!_configured)
        {
            return default;
        }

        LastStep = _brain.Tick(_grid, perception);
        _body.GlobalPosition = WorldOf(LastStep.Position);
        return LastStep;
    }

    private Vector3 WorldOf(TrueTile tile) => new(
        _originWorld.X + ((tile.X - _origin.X) * _tileSize),
        _originWorld.Y,
        _originWorld.Z + ((tile.Y - _origin.Y) * _tileSize));
}
