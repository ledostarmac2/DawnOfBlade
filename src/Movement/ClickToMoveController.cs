using Godot;
using System.Collections.Generic;
using System.Linq;

namespace DawnOfBlade.Movement;

public sealed class ClickToMoveController
{
    public float MoveSpeed { get; set; } = 5.0f;
    public float ArrivalDistance { get; set; } = 0.12f;
    public Vector3? TargetPosition { get; private set; }
    public Vector3? DestinationPosition { get; private set; }

    private readonly Queue<Vector3> _waypoints = new();

    public void SetTargetPosition(Vector3 targetPosition)
    {
        _waypoints.Clear();
        TargetPosition = targetPosition;
        DestinationPosition = targetPosition;
    }

    public void SetPath(IEnumerable<Vector3> waypoints)
    {
        _waypoints.Clear();
        var path = waypoints.ToArray();
        foreach (var waypoint in path)
        {
            _waypoints.Enqueue(waypoint);
        }

        DestinationPosition = path.Length > 0 ? path[^1] : null;
        AdvanceWaypoint();
    }

    public void ClearTarget()
    {
        _waypoints.Clear();
        TargetPosition = null;
        DestinationPosition = null;
    }

    public Vector3 GetVelocity(Vector3 currentPosition)
    {
        if (TargetPosition is null)
        {
            return Vector3.Zero;
        }

        var offset = TargetPosition.Value - currentPosition;
        offset.Y = 0;

        if (offset.Length() <= ArrivalDistance)
        {
            AdvanceWaypoint();
            if (TargetPosition is null)
            {
                return Vector3.Zero;
            }

            offset = TargetPosition.Value - currentPosition;
            offset.Y = 0;
        }

        // TODO: Route through Godot NavigationAgent3D once the prototype map has a navmesh.
        return offset.Normalized() * MoveSpeed;
    }

    private void AdvanceWaypoint()
    {
        TargetPosition = _waypoints.Count > 0 ? _waypoints.Dequeue() : null;
        if (TargetPosition is null)
        {
            DestinationPosition = null;
        }
    }
}
