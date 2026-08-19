using System.Diagnostics;
using GoQuake2.Client;
using GoQuake2.Q2File;
using GoQuake2.Render;
using OpenTK.Mathematics;

namespace GoQuake2.Engine;

/// <summary>
/// Renderer/runtime do mapa sem ownership de janela. A UI (GLControl/WinForms)
/// fornece o contexto OpenGL e chama Update/Render na thread da interface.
/// </summary>
public sealed class Game : IDisposable
{
    private readonly List<BlasterProjectile> blasterProjectiles = new();
    public const float MouseSensitivity = 0.7f;
    public const float CameraSpeed = 200f;
    public const float CameraFov = 70f;
    public const float NearPlane = 0.1f;
    public const float FarPlane = 4096f;
    public const ushort ClusterInvalidId = 65535;

    private const string TexturePath = "textures/";
    private const string TextureExtension = ".wal";

    private readonly Quake2ViewerService service;
    private readonly string mapName;
    private readonly InputHandler input = new();
    private readonly Stopwatch fps = Stopwatch.StartNew();

    private Renderer renderer = null!;
    private Camera camera = null!;
    private MapData map = null!;
    private MapTexture[] textures = [];
    private BspTree bsp = null!;
    private RenderMap renderMap = new();
    private int previousLeaf = -1;
    private int frames;
    private bool initialized;

    public Game(Quake2ViewerService service, string mapName)
    {
        this.service = service;
        this.mapName = mapName;
    }

    public void FireBlaster()
    {
        Vector3 origin = camera.Position;
        Vector3 direction = camera.Forward;

        origin += direction * 8f;

        blasterProjectiles.Add(
            new BlasterProjectile(
                origin,
                direction * 900f));
    }

    public bool IsFlying => initialized && camera.IsFlying;

    public void Initialize(int width, int height)
    {
        if (initialized)
        {
            return;
        }

        renderer = new Renderer();
        LoadGameData();
        camera = new Camera(input, map);
        renderer.Resize(Math.Max(1, width), Math.Max(1, height));
        initialized = true;
    }

    public void Update(double dt, InputState keys, Vector2 mouseDelta)
    {
        if (!initialized)
        {
            return;
        }

        camera.Update(dt, keys, mouseDelta);

        UpdateBlasterProjectiles(dt);

        var leaf = bsp.FindLeafNode(0, map, camera.GetCameraPosition());
        if (previousLeaf == leaf.LeafIndex)
        {
            return;
        }

        if (leaf.Faces.Length > 0)
        {
            renderMap = MapBuilder.Create(map, textures, leaf.Faces, renderer.Textures);
        }

        previousLeaf = leaf.LeafIndex;
    }

    private void RenderBlasterProjectiles(int width, int height)
    {
        if (blasterProjectiles.Count == 0)
        {
            return;
        }

        Matrix4 view = camera.GetViewMatrix();
        Matrix4 projection = camera.GetPerspectiveMatrix(width, height);

        foreach (BlasterProjectile projectile in blasterProjectiles)
        {
            Vector3 direction = projectile.Velocity.Normalized();

            // Pequeno bolt/tracer, em vez de uma linha ocupando o mapa inteiro.
            Vector3 head = projectile.Position;
            Vector3 tail = head - direction * 48f;

            renderer.DrawBlaster(tail, head, view, projection);
        }
    }

    private void UpdateBlasterProjectiles(double dt)
    {
        for (int i = blasterProjectiles.Count - 1; i >= 0; i--)
        {
            BlasterProjectile projectile = blasterProjectiles[i];

            projectile.Position +=
                projectile.Velocity * (float)dt;

            projectile.Lifetime -= (float)dt;

            if (projectile.Lifetime <= 0f)
            {
                blasterProjectiles.RemoveAt(i);
            }
        }
    }

    public void Render(int width, int height)
    {
        if (!initialized)
        {
            return;
        }

        renderer.DrawSkybox();
        renderer.Prepare(
            camera.GetViewMatrix(),
            camera.GetPerspectiveMatrix(width, height));
        renderer.Draw(renderMap);
        RenderBlasterProjectiles(width, height);

        UpdateFpsCounter();
    }

    public void Resize(int width, int height)
    {
        if (initialized)
        {
            renderer.Resize(Math.Max(1, width), Math.Max(1, height));
        }
    }

    public void Dispose()
    {
        if (!initialized)
        {
            return;
        }

        renderer.Dispose();
        initialized = false;
    }

    private void LoadGameData()
    {
        map = service.LoadMap(mapName);
        Console.WriteLine($"BSP map successfully loaded: {mapName}");

        textures = CreateTextureList(map.TextureIds);
        Console.WriteLine("Textures successfully loaded");

        bsp = new BspTree(map);
        Console.WriteLine("BSP Tree built successfully");
    }

    private MapTexture[] CreateTextureList(Dictionary<string, int> textureIds)
    {
        var result = new MapTexture[textureIds.Count];

        foreach (var entry in textureIds)
        {
            string fullName = (TexturePath + entry.Key.Trim() + TextureExtension).ToLowerInvariant();

            try
            {
                var (image, header) = service.LoadWal(fullName);
                int texture = renderer.Textures.CreateWalTexture(image, header);
                result[entry.Value] = new MapTexture(texture, header.Width, header.Height);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: texture {fullName} is missing: {ex.Message}");
                result[entry.Value] = new MapTexture(0, 0, 0);
            }
        }

        return result;
    }

    private void UpdateFpsCounter()
    {
        frames++;

        if (fps.Elapsed.TotalSeconds < 1d)
        {
            return;
        }

        Console.WriteLine(
            $"FPS: {frames / fps.Elapsed.TotalSeconds:F1} | " +
            $"Mode: {(camera.IsFlying ? "FLY" : "FPS")}");

        frames = 0;
        fps.Restart();
    }
}
