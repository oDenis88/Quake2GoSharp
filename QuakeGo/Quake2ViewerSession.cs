using System.Windows.Forms;

namespace GoQuake2;

public sealed class Quake2ViewerSession : IDisposable
{
    private readonly Form form;
    private readonly TaskCompletionSource<bool> completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal Quake2ViewerSession(Form form)
    {
        this.form = form;
        form.FormClosed += (_, _) => completion.TrySetResult(true);
    }

    public Task Completion => completion.Task;
    public Form Window => form;

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
        Close();
    }
}
