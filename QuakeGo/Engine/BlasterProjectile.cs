using OpenTK.Mathematics;

namespace GoQuake2.Engine;

public sealed class BlasterProjectile
{
    public Vector3 Position { get; set; }

    public Vector3 Velocity { get; }

    public float Lifetime { get; set; } = 2.0f;

    public BlasterProjectile(
        Vector3 position,
        Vector3 velocity)
    {
        Position = position;
        Velocity = velocity;
    }
}