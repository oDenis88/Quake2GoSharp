using GoQuake2;
using QuakeReader.Audio;

namespace QuakeReader;

public sealed class Quake2ViewerSession : IDisposable
{
    private readonly MciMusicPlayer musicPlayer = new();
    private readonly Form form;
    private readonly TaskCompletionSource<bool> completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    public event EventHandler? Closed;
    public Quake2ViewerSession(
        Quake2ViewerService service,
        string mapName,
        Quake2ViewerOptions? options = null)
    {
        options ??= new Quake2ViewerOptions();
        string normalized = service.NormalizeMapName(mapName);

        form = new Form
        {
            Text = $"{options.WindowTitle} - {normalized}",
            ClientSize = new Size(options.Width, options.Height),
            StartPosition = FormStartPosition.CenterScreen,
            KeyPreview = true
        };

        form.FormClosed += (_, _) =>
        {
            Closed?.Invoke(this, EventArgs.Empty);
        };


        var control = new Quake2MapControl(
            service,
            normalized,
            options,
            () => form.Close());

        form.Controls.Add(control);

        string musicPath = Path.Combine(
            AppContext.BaseDirectory,
            "music",
            "map.mp3");

        var helpLabel = new Label
        {
            AutoSize = true,
            Text =
            "Use W, A, S, D to move around\r\n" +
            "Hold Spacebar to fly around",

            ForeColor = Color.White,
            BackColor = Color.FromArgb(32, 32, 32),

            Font = new Font(
                "Segoe UI",
                10f,
                FontStyle.Regular),

            Padding = new Padding(6),
            Location = new Point(
            12,
            form.ClientSize.Height - 65),

            Anchor =
            AnchorStyles.Left |
            AnchorStyles.Bottom
            };

        form.Controls.Add(helpLabel);
        helpLabel.BringToFront();

        musicPlayer.Play(musicPath, loop: true);

        form.FormClosed += (_, _) => completion.TrySetResult(true);
    }

    public Task Completion => completion.Task;
    public Form Window => form;

    public void Show(IWin32Window? owner = null)
    {
        if (owner is null)
        {
            form.Show();
        }
        else
        {
            form.Show(owner);
        }
    }

    public void Close()
    {
        if (form.IsDisposed)
        {
            return;
        }

        if (form.InvokeRequired)
        {
            form.BeginInvoke(Close);
            return;
        }

        form.Close();
    }

    public void Dispose()
    {
        musicPlayer.Dispose();
        Close();
    }
}
