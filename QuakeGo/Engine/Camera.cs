using GoQuake2.Client;
using GoQuake2.Q2File;
using OpenTK.Mathematics;

namespace GoQuake2.Engine;

public sealed class Camera
{
    private const float ViewHeight = 22f;
    private const float MaxPitch = 89f;

    private readonly InputHandler input;
    private readonly PlayerPhysics physics;

    private float pitch;
    private float yaw;
    private Vector3 playerOrigin;

    public Camera(InputHandler input, MapData map)
    {
        this.input = input;
        physics = new PlayerPhysics(map);

        var spawn = FindPlayerSpawn(map);
        playerOrigin = physics.FindSafeSpawn(spawn.Origin);
        yaw = spawn.Yaw;
        pitch = 0f;
    }

    public bool IsFlying { get; private set; }

    public Matrix4 GetViewMatrix()
    {
        Vector3 eye = GetEyePosition();
        Vector3 forward = GetForward(includePitch: true);

        return Matrix4.LookAt(
            eye,
            eye + forward,
            Vector3.UnitZ);
    }

    public Matrix4 GetPerspectiveMatrix(int width, int height)
    {
        return Matrix4.CreatePerspectiveFieldOfView(
            MathHelper.DegreesToRadians(Game.CameraFov),
            Math.Max(1, width) / (float)Math.Max(1, height),
            Game.NearPlane,
            Game.FarPlane);
    }

    public void Update(double dt, InputState keys, Vector2 mouseDelta)
    {
        UpdateLook(mouseDelta);

        IsFlying = input.IsActive(PlayerAction.Fly, keys);

        float forwardInput = 0f;
        float rightInput = 0f;

        if (input.IsActive(PlayerAction.Forward, keys))
        {
            forwardInput += 1f;
        }

        if (input.IsActive(PlayerAction.Backward, keys))
        {
            forwardInput -= 1f;
        }

        if (input.IsActive(PlayerAction.Right, keys))
        {
            rightInput += 1f;
        }

        if (input.IsActive(PlayerAction.Left, keys))
        {
            rightInput -= 1f;
        }

        Vector2 moveInput = new(rightInput, forwardInput);
        if (moveInput.LengthSquared > 1f)
        {
            moveInput = moveInput.Normalized();
        }

        float speed = (float)(Game.CameraSpeed * dt);

        if (IsFlying)
        {
            physics.ResetVerticalVelocity();

            Vector3 flyForward = GetForward(includePitch: true);
            Vector3 flyRight = GetRight();
            Vector3 delta = (flyForward * moveInput.Y + flyRight * moveInput.X) * speed;

            playerOrigin += delta;
            return;
        }

        Vector3 groundForward = GetForward(includePitch: false);
        Vector3 groundRight = GetRight();
        Vector3 horizontalDelta =
            (groundForward * moveInput.Y + groundRight * moveInput.X) * speed;

        horizontalDelta.Z = 0f;
        playerOrigin = physics.MoveGrounded(playerOrigin, horizontalDelta, dt);
    }

    public float[] GetCameraPosition()
    {
        Vector3 eye = GetEyePosition();
        return [eye.X, eye.Y, eye.Z];
    }

    public Vector3 GetPlayerOrigin()
    {
        return playerOrigin;
    }

    private Vector3 GetEyePosition()
    {
        return playerOrigin + Vector3.UnitZ * ViewHeight;
    }

    private Vector3 GetForward(bool includePitch)
    {
        float usedPitch = includePitch ? pitch : 0f;
        float cosPitch = MathF.Cos(usedPitch);

        return new Vector3(
            MathF.Cos(yaw) * cosPitch,
            MathF.Sin(yaw) * cosPitch,
            MathF.Sin(usedPitch)).Normalized();
    }

    private Vector3 GetRight()
    {
        return new Vector3(
            MathF.Sin(yaw),
            -MathF.Cos(yaw),
            0f).Normalized();
    }

    private void UpdateLook(Vector2 mouseDelta)
    {
        // Quake usa Z como eixo vertical. Mouse para a direita deve girar o yaw
        // no sentido horario visto de cima; mouse para cima aumenta o pitch.
        yaw -= (float)(mouseDelta.X * Game.MouseSensitivity * 0.025);
        pitch -= (float)(mouseDelta.Y * Game.MouseSensitivity * 0.025);

        yaw %= MathF.Tau;
        if (yaw < 0f)
        {
            yaw += MathF.Tau;
        }

        float maxPitchRadians = MathHelper.DegreesToRadians(MaxPitch);
        pitch = Math.Clamp(pitch, -maxPitchRadians, maxPitchRadians);
    }

    private static (Vector3 Origin, float Yaw) FindPlayerSpawn(MapData map)
    {
        string[] preferredClasses =
        [
            "info_player_start",
            "info_player_deathmatch",
            "info_player_coop"
        ];

        foreach (string className in preferredClasses)
        {
            foreach (MapEntity entity in map.Entities)
            {
                if (!entity.ClassName.Equals(className, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (MapEntityParser.TryGetOrigin(entity, out Vector3 origin))
                {
                    return (
                        origin,
                        MapEntityParser.GetAngleRadians(entity, MathF.PI));
                }
            }
        }

        // Fallback mantido apenas para BSPs sem entidade de spawn.
        return (new Vector3(50f, -256f, 50f), 0f);
    }
}
