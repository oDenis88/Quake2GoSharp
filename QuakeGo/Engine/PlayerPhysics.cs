using GoQuake2.Q2File;
using OpenTK.Mathematics;

namespace GoQuake2.Engine;

public sealed class PlayerPhysics
{
    private const uint ContentsSolid = 1;
    private const float CollisionEpsilon = 0.05f;
    private const float MaxSubStep = 4f;

    private static readonly Vector3 PlayerMins = new(-16f, -16f, -24f);
    private static readonly Vector3 PlayerMaxs = new(16f, 16f, 32f);

    private readonly MapData map;
    private float verticalVelocity;

    public PlayerPhysics(MapData map)
    {
        this.map = map;
    }

    public Vector3 MoveGrounded(Vector3 origin, Vector3 horizontalDelta, double dt)
    {
        var flatDelta = new Vector3(horizontalDelta.X, horizontalDelta.Y, 0f);
        var moved = MoveHorizontal(origin, flatDelta);

        if ((moved - origin).LengthSquared < flatDelta.LengthSquared * 0.25f && flatDelta.LengthSquared > 0.001f)
        {
            moved = TryStep(origin, flatDelta, moved);
        }

        verticalVelocity = Math.Max(verticalVelocity - (800f * (float)dt), -800f);
        var beforeFall = moved;
        moved = SweepAxis(moved, Vector3.UnitZ, verticalVelocity * (float)dt);

        if (Math.Abs(moved.Z - beforeFall.Z) < Math.Abs(verticalVelocity * (float)dt) - 0.001f)
        {
            verticalVelocity = 0f;
        }

        return moved;
    }

    public void ResetVerticalVelocity()
    {
        verticalVelocity = 0f;
    }

    public Vector3 FindSafeSpawn(Vector3 requested)
    {
        if (!Collides(requested))
        {
            return requested;
        }

        for (float z = 1f; z <= 128f; z += 1f)
        {
            var candidate = requested + (Vector3.UnitZ * z);
            if (!Collides(candidate))
            {
                return candidate;
            }
        }

        return requested;
    }

    private Vector3 MoveHorizontal(Vector3 origin, Vector3 delta)
    {
        var result = SweepAxis(origin, Vector3.UnitX, delta.X);
        result = SweepAxis(result, Vector3.UnitY, delta.Y);
        return result;
    }

    private Vector3 TryStep(Vector3 origin, Vector3 delta, Vector3 noStepResult)
    {
        const float stepHeight = 18f;

        var raised = SweepAxis(origin, Vector3.UnitZ, stepHeight);
        if (raised.Z < origin.Z + stepHeight - 0.5f)
        {
            return noStepResult;
        }

        var stepped = MoveHorizontal(raised, delta);
        stepped = SweepAxis(stepped, Vector3.UnitZ, -(stepHeight + 2f));

        float noStepDistance = new Vector2(noStepResult.X - origin.X, noStepResult.Y - origin.Y).LengthSquared;
        float stepDistance = new Vector2(stepped.X - origin.X, stepped.Y - origin.Y).LengthSquared;

        return stepDistance > noStepDistance ? stepped : noStepResult;
    }

    private Vector3 SweepAxis(Vector3 origin, Vector3 axis, float distance)
    {
        if (Math.Abs(distance) < 0.0001f)
        {
            return origin;
        }

        int steps = Math.Max(1, (int)Math.Ceiling(Math.Abs(distance) / MaxSubStep));
        float stepDistance = distance / steps;
        var current = origin;

        for (int i = 0; i < steps; i++)
        {
            var candidate = current + (axis * stepDistance);
            if (Collides(candidate))
            {
                break;
            }

            current = candidate;
        }

        return current;
    }

    private bool Collides(Vector3 origin)
    {
        foreach (var brush in map.Brushes)
        {
            if ((brush.Contents & ContentsSolid) == 0 || brush.NumSides == 0)
            {
                continue;
            }

            if (IntersectsBrush(origin, brush))
            {
                return true;
            }
        }

        return false;
    }

    private bool IntersectsBrush(Vector3 origin, Brush brush)
    {
        int firstSide = checked((int)brush.FirstSide);
        int sideCount = checked((int)brush.NumSides);

        for (int i = 0; i < sideCount; i++)
        {
            var brushSide = map.BrushSides[firstSide + i];
            var plane = map.Planes[brushSide.Plane];
            var normal = new Vector3(plane.Normal[0], plane.Normal[1], plane.Normal[2]);

            var minCorner = new Vector3(
                normal.X >= 0f ? PlayerMins.X : PlayerMaxs.X,
                normal.Y >= 0f ? PlayerMins.Y : PlayerMaxs.Y,
                normal.Z >= 0f ? PlayerMins.Z : PlayerMaxs.Z);

            float nearestDistance = Vector3.Dot(origin + minCorner, normal) - plane.Distance;

            if (nearestDistance >= -CollisionEpsilon)
            {
                return false;
            }
        }

        return true;
    }
}
