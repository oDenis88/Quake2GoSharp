using GoQuake2;

namespace QuakeReader;

public partial class frmMain : Form
{
    private readonly Quake2ViewerService quake = new();
    private Quake2ViewerSession? viewer;

    public frmMain()
    {
        InitializeComponent();
    }

    private void btnLoadPak_Click(object sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Quake II PAK (*.pak)|*.pak"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        quake.LoadPak(dialog.FileName);
        txtPath.Text = dialog.FileName;
        lstBsps.DataSource = quake.Maps.ToList();
        btnLoadPak.Enabled = false;
    }

    private void frmMain_Load(object sender, EventArgs e)
    {
        txtPath.Enabled = false;
    }

    private void btnLoadBsp_Click(object sender, EventArgs e)
    {
        if (lstBsps.SelectedItem is not string map)
        {
            return;
        }

        viewer?.Dispose();

        viewer = new Quake2ViewerSession(
            quake,
            map,
            new Quake2ViewerOptions
            {
                Width = 1280,
                Height = 720,
                WindowTitle = "Visualizador Quake II",
                VSync = false
            });

        viewer.Closed += Viewer_Closed;

        viewer.Show(this);
    }

    private void Viewer_Closed(object? sender, EventArgs e)
    {
        btnLoadBsp.Enabled = true;

        if (viewer != null)
        {
            viewer.Closed -= Viewer_Closed;
            viewer.Dispose();
            viewer = null;
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        viewer?.Dispose();
        base.OnFormClosed(e);
    }
}
