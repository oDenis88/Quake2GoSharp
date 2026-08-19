using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using GoQuake2;
using GoQuake2.Client;
using GoQuake2.Engine;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using QuakeReader.Audio;
using GLHostControl = OpenTK.GLControl.GLControl;

namespace QuakeReader;

/// <summary>
/// Host WinForms do renderer. Esta classe fica no executavel WinForms;
/// a DLL GoQuake2 permanece independente de System.Windows.Forms.
/// </summary>
public sealed class Quake2MapControl : GLHostControl
{
    private readonly Game game;
    private readonly InputState input = new();
    private readonly System.Windows.Forms.Timer renderTimer;
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private readonly Action? closeRequested;
    private readonly WaveSoundPlayer? blasterSound;
    private readonly WaveSoundPlayer[] footstepSounds;
    private readonly WaveSoundPlayer? jumpSound;

    private const float FootstepDistance = 48f;
    private float footstepDistanceAccumulator;
    private int nextFootstepIndex;
    private Vector3 lastFootstepPosition;
    private bool hasFootstepPosition;

    private Vector2 mouseDelta;
    private Point lastMousePosition;
    private bool hasMousePosition;
    private double lastFrameTime;
    private bool disposed;

    public Quake2MapControl(
        Quake2ViewerService service,
        string mapName,
        Quake2ViewerOptions options,
        Action? closeRequested = null,
        Action? toggleMusicRequested = null)
    {
        this.closeRequested = closeRequested;
        this.toggleMusicRequested = toggleMusicRequested;

        API = ContextAPI.OpenGL;
        APIVersion = new Version(3, 3);
        Profile = ContextProfile.Core;
        Flags = ContextFlags.ForwardCompatible;
        IsEventDriven = false;

        Dock = DockStyle.Fill;
        TabStop = true;
        BackColor = Color.Black;

        game = new Game(service, service.NormalizeMapName(mapName));

        if (service.TryLoadFileBytes("sound/weapons/blastf1a.wav", out byte[] blasterWave))
        {
            blasterSound = new WaveSoundPlayer(blasterWave);
        }

        footstepSounds = LoadFootstepSounds(service);
        jumpSound = TryLoadSound(
            service,
            "sound/player/male/jump1.wav",
            "sound/player/jump1.wav",
            "sound/players/male/jump1.wav");

        KeyDown += OnViewerKeyDown;
        KeyUp += OnViewerKeyUp;
        MouseDown += OnViewerMouseDown;
        MouseMove += OnViewerMouseMove;
        MouseEnter += (_, _) => Focus();
        LostFocus += (_, _) =>
        {
            input.Clear();
            hasMousePosition = false;
        };

        renderTimer = new System.Windows.Forms.Timer
        {
            Interval = 8
        };

        renderTimer.Tick += (_, _) => Invalidate();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        if (IsDesignMode || !HasValidContext)
        {
            return;
        }

        MakeCurrent();
        game.Initialize(ClientSize.Width, ClientSize.Height);
        lastFootstepPosition = game.PlayerOrigin;
        hasFootstepPosition = true;
        lastFrameTime = clock.Elapsed.TotalSeconds;
        renderTimer.Start();
        Focus();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (IsDesignMode || !HasValidContext || disposed)
        {
            base.OnPaint(e);
            return;
        }

        MakeCurrent();

        double now = clock.Elapsed.TotalSeconds;
        double dt = Math.Clamp(now - lastFrameTime, 0d, 0.1d);
        lastFrameTime = now;

        Vector2 delta = mouseDelta;
        mouseDelta = Vector2.Zero;

        game.Update(dt, input, delta);
        UpdateFootsteps();
        game.Render(
            Math.Max(1, ClientSize.Width),
            Math.Max(1, ClientSize.Height));

        SwapBuffers();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);

        if (!HasValidContext || disposed)
        {
            return;
        }

        MakeCurrent();
        game.Resize(ClientSize.Width, ClientSize.Height);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !disposed)
        {
            renderTimer.Stop();
            renderTimer.Dispose();
            blasterSound?.Dispose();
            jumpSound?.Dispose();

            foreach (WaveSoundPlayer footstepSound in footstepSounds)
            {
                footstepSound.Dispose();
            }

            if (HasValidContext)
            {
                MakeCurrent();
                game.Dispose();
            }

            disposed = true;
        }

        base.Dispose(disposing);
    }
    private readonly Action? toggleMusicRequested;
    private void OnViewerKeyDown(object? sender, KeyEventArgs e)
    {
        bool spaceWasDown = input.IsDown(PlayerKey.Space);

        if (TryMapKey(e.KeyCode, out PlayerKey key))
        {
            input.KeyDown(key);
        }

        if (e.KeyCode == Keys.Space && !spaceWasDown)
        {
            jumpSound?.Play();
            footstepDistanceAccumulator = 0f;
        }

        if (e.KeyCode == Keys.M)
        {
            toggleMusicRequested?.Invoke();
            e.Handled = true;
        }

        if (e.KeyCode == Keys.Escape)
        {
            closeRequested?.Invoke();
            e.Handled = true;
        }
    }

    private void OnViewerKeyUp(object? sender, KeyEventArgs e)
    {
        if (TryMapKey(e.KeyCode, out PlayerKey key))
        {
            input.KeyUp(key);
        }
    }

    private void OnViewerMouseDown(object? sender, MouseEventArgs e)
    {
        Focus();

        lastMousePosition = e.Location;
        hasMousePosition = true;

        switch (e.Button)
        {
            case MouseButtons.Left:
                game.FireBlaster();
                blasterSound?.Play();
                break;
        }
    }

    private void OnViewerMouseMove(object? sender, MouseEventArgs e)
    {
        if (!Focused)
        {
            return;
        }

        if (!hasMousePosition)
        {
            lastMousePosition = e.Location;
            hasMousePosition = true;
            return;
        }

        mouseDelta.X += e.X - lastMousePosition.X;
        mouseDelta.Y += e.Y - lastMousePosition.Y;
        lastMousePosition = e.Location;
    }

    private void UpdateFootsteps()
    {
        Vector3 currentPosition = game.PlayerOrigin;

        if (!hasFootstepPosition)
        {
            lastFootstepPosition = currentPosition;
            hasFootstepPosition = true;
            return;
        }

        Vector3 delta = currentPosition - lastFootstepPosition;
        lastFootstepPosition = currentPosition;

        if (game.IsFlying || !game.IsGrounded || footstepSounds.Length == 0)
        {
            footstepDistanceAccumulator = 0f;
            return;
        }

        float horizontalDistance = new Vector2(delta.X, delta.Y).Length;

        // Ignore discontinuities such as spawn correction or future teleports.
        if (horizontalDistance > 64f)
        {
            footstepDistanceAccumulator = 0f;
            return;
        }

        footstepDistanceAccumulator += horizontalDistance;

        if (footstepDistanceAccumulator < FootstepDistance)
        {
            return;
        }

        footstepDistanceAccumulator %= FootstepDistance;

        // SND_NOSTOP keeps a footstep from cutting a blaster/jump already playing.
        footstepSounds[nextFootstepIndex].Play(doNotInterrupt: true);
        nextFootstepIndex = (nextFootstepIndex + 1) % footstepSounds.Length;
    }

    private static WaveSoundPlayer[] LoadFootstepSounds(Quake2ViewerService service)
    {
        var sounds = new List<WaveSoundPlayer>();

        for (int i = 1; i <= 4; i++)
        {
            WaveSoundPlayer? sound = TryLoadSound(
                service,
                $"sound/player/step{i}.wav",
                $"sound/player/male/step{i}.wav",
                $"sound/players/male/step{i}.wav");

            if (sound != null)
            {
                sounds.Add(sound);
            }
        }

        return sounds.ToArray();
    }

    private static WaveSoundPlayer? TryLoadSound(
        Quake2ViewerService service,
        params string[] candidates)
    {
        foreach (string path in candidates)
        {
            if (service.TryLoadFileBytes(path, out byte[] wave))
            {
                return new WaveSoundPlayer(wave);
            }
        }

        return null;
    }

    private static bool TryMapKey(Keys keyCode, out PlayerKey key)
    {
        switch (keyCode)
        {
            case Keys.W:
                key = PlayerKey.W;
                return true;

            case Keys.A:
                key = PlayerKey.A;
                return true;

            case Keys.S:
                key = PlayerKey.S;
                return true;

            case Keys.D:
                key = PlayerKey.D;
                return true;

            case Keys.Space:
                key = PlayerKey.Space;
                return true;

            case Keys.Escape:
                key = PlayerKey.Escape;
                return true;

            default:
                key = default;
                return false;
        }
    }
}
