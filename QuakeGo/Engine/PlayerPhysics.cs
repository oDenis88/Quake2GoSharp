using GoQuake2.Q2File;
using OpenTK.Mathematics;

namespace GoQuake2.Engine;

public sealed class PlayerPhysics
{
    private const uint ContentsSolid = 1;
    private const float CollisionEpsilon = 0.05f;
    private const float MaxSubStep = 4f;

    // Deliberately smaller than Quake II's normal player bbox.
    // This is a map viewer, so narrow corridors/door frames should be easy to navigate.
    private static readonly Vector3 PlayerMins = new(-8f, -8f, -16f);
    private static readonly Vector3 PlayerMaxs = new(8f, 8f, 24f);

    private readonly MapData map;
    private readonly int[] worldBrushIndices;
    private float verticalVelocity;

    public PlayerPhysics(MapData map)
    {
        this.map = map;
        worldBrushIndices = CollectWorldBrushIndices(map);
    }

    public Vector3 MoveGrounded(Vector3 origin, Vector3 horizontalDelta, double dt)
    {
        var flatDelta = new Vector3(horizontalDelta.X, horizontalDelta.Y, 0f);
        var moved = MoveHorizontal(origin, flatDelta);

        if ((moved - origin).LengthSquared < flatDelta.LengthSquared * 0.25f &&
            flatDelta.LengthSquared > 0.001f)
        {
            moved = TryStep(origin, flatDelta, moved);
        }

        verticalVelocity = Math.Max(verticalVelocity - (800f * (float)dt), -800f);

        var beforeFall = moved;
        float fallDistance = verticalVelocity * (float)dt;
        moved = SweepAxis(moved, Vector3.UnitZ, fallDistance);

        if (Math.Abs(moved.Z - beforeFall.Z) < Math.Abs(fallDistance) - 0.001f)
        {
            verticalVelocity = 0f;
        }

        return moved;
    }

    public void ResetVerticalVelocity()
    {
        verticalVelocity = 0f;
    }

    /// <summary>
    /// Called when leaving noclip/free-roam. If the camera was released while
    /// slightly inside world geometry, move it to a nearby valid point first;
    /// gravity can then take over normally.
    /// </summary>
    public Vector3 PrepareForGroundedMode(Vector3 origin)
    {
        verticalVelocity = 0f;

        if (!Collides(origin))
        {
            return origin;
        }

        // Most common case after noclip: player is a little inside floor/ceiling.
        for (float z = 1f; z <= 64f; z += 1f)
        {
            var candidate = origin + Vector3.UnitZ * z;
            if (!Collides(candidate))
            {
                return candidate;
            }
        }

        // Then try a small horizontal ring. This avoids remaining trapped if
        // Space is released while intersecting a wall.
        Vector2[] directions =
        [
            Vector2.UnitX,
            -Vector2.UnitX,
            Vector2.UnitY,
            -Vector2.UnitY,
            new Vector2(1f, 1f).Normalized(),
            new Vector2(1f, -1f).Normalized(),
            new Vector2(-1f, 1f).Normalized(),
            new Vector2(-1f, -1f).Normalized()
        ];

        for (float radius = 2f; radius <= 48f; radius += 2f)
        {
            foreach (Vector2 direction in directions)
            {
                var candidate = origin + new Vector3(
                    direction.X * radius,
                    direction.Y * radius,
                    0f);

                if (!Collides(candidate))
                {
                    return candidate;
                }
            }
        }

        // Do not teleport somewhere arbitrary. If there is no nearby safe point,
        // keep the current noclip position and let the next frames resolve it.
        return origin;
    }

    public Vector3 FindSafeSpawn(Vector3 requested)
    {
        if (!Collides(requested))
        {
            return requested;
        }

        for (float z = 1f; z <= 128f; z += 1f)
        {
            var candidate = requested + Vector3.UnitZ * z;
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

        float noStepDistance = new Vector2(
            noStepResult.X - origin.X,
            noStepResult.Y - origin.Y).LengthSquared;

        float stepDistance = new Vector2(
            stepped.X - origin.X,
            stepped.Y - origin.Y).LengthSquared;

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
            var candidate = current + axis * stepDistance;
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
        foreach (int brushIndex in worldBrushIndices)
        {
            Brush brush = map.Brushes[brushIndex];

            // Only actual structural solid world brushes block this viewer.
            // PLAYERCLIP, MONSTERCLIP, liquids, triggers and brush submodels are ignored.
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

        if (firstSide < 0 || firstSide + sideCount > map.BrushSides.Length)
        {
            return false;
        }

        for (int i = 0; i < sideCount; i++)
        {
            BrushSide brushSide = map.BrushSides[firstSide + i];

            if (brushSide.Plane >= map.Planes.Length)
            {
                return false;
            }

            Plane plane = map.Planes[brushSide.Plane];
            var normal = new Vector3(
                plane.Normal[0],
                plane.Normal[1],
                plane.Normal[2]);

            var minCorner = new Vector3(
                normal.X >= 0f ? PlayerMins.X : PlayerMaxs.X,
                normal.Y >= 0f ? PlayerMins.Y : PlayerMaxs.Y,
                normal.Z >= 0f ? PlayerMins.Z : PlayerMaxs.Z);

            float nearestDistance =
                Vector3.Dot(origin + minCorner, normal) - plane.Distance;

            if (nearestDistance >= -CollisionEpsilon)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Returns only brushes reachable from BSP model 0 (worldspawn).
    /// Submodels (*1, *2, ...) are brush entities and are intentionally excluded.
    /// </summary>
    private static int[] CollectWorldBrushIndices(MapData map)
    {
        if (map.Models.Length == 0 || map.Nodes.Length == 0)
        {
            // Compatibility fallback for an unusual/old parsed map.
            return map.LeafBrushes
                .Select(index => (int)index)
                .Where(index => index >= 0 && index < map.Brushes.Length)
                .Distinct()
                .ToArray();
        }

        int worldHeadNode = map.Models[0].HeadNode;
        var brushes = new HashSet<int>();
        var visitedNodes = new HashSet<int>();
        var visitedLeaves = new HashSet<int>();
        var pending = new Stack<int>();
        pending.Push(worldHeadNode);

        while (pending.Count > 0)
        {
            int child = pending.Pop();

            if (child >= 0)
            {
                if (child >= map.Nodes.Length || !visitedNodes.Add(child))
                {
                    continue;
                }

                BspNode node = map.Nodes[child];
                pending.Push(node.FrontChild);
                pending.Push(node.BackChild);
                continue;
            }

            int leafIndex = -(child + 1);
            if (leafIndex < 0 ||
                leafIndex >= map.BspLeaves.Length ||
                !visitedLeaves.Add(leafIndex))
            {
                continue;
            }

            BspLeaf leaf = map.BspLeaves[leafIndex];
            int first = leaf.FirstLeafBrush;
            int count = leaf.NumLeafBrushes;

            for (int i = 0; i < count; i++)
            {
                int leafBrushIndex = first + i;
                if (leafBrushIndex < 0 || leafBrushIndex >= map.LeafBrushes.Length)
                {
                    continue;
                }

                int brushIndex = map.LeafBrushes[leafBrushIndex];
                if (brushIndex >= 0 && brushIndex < map.Brushes.Length)
                {
                    brushes.Add(brushIndex);
                }
            }
        }

        return brushes.ToArray();
    }
}
