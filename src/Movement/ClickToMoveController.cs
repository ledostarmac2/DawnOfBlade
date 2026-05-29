using Godot;

namespace DawnOfBlade.Movement;

public sealed class ClickToMoveController
{
    public float MoveSpeed { get; set; } = 5.0f;
    public float ArrivalDistance { get; set; } = 0.12f;
    public Vector3? TargetPosition { get; private set; }

    public void SetTargetPosition(Vector3 targetPosition)
    {
        TargetPosition = targetPosition;
    }

    public void ClearTarget()
    {
        TargetPosition = null;
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
            ClearTarget();
            return Vector3.Zero;
        }

        // TODO: Route through Godot NavigationAgent3D once the prototype map has a navmesh.
        return offset.Normalized() * MoveSpeed;
    }
}

