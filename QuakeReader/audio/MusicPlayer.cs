using System.Runtime.InteropServices;
using System.Text;

namespace QuakeReader.Audio;

public sealed class MciMusicPlayer : IDisposable
{
    [DllImport("winmm.dll", CharSet = CharSet.Auto)]
    private static extern int mciSendString(
        string command,
        StringBuilder? returnValue,
        int returnLength,
        IntPtr winHandle);

    private const string Alias = "quake_music";

    private bool opened;

    public void Play(string filePath, bool loop = true)
    {
        Stop();

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                "Arquivo de música não encontrado.",
                filePath);
        }

        string fullPath = Path.GetFullPath(filePath);

        int result = mciSendString(
            $"open \"{fullPath}\" alias {Alias}",
            null,
            0,
            IntPtr.Zero);

        if (result != 0)
        {
            throw new InvalidOperationException(
                $"MCI não conseguiu abrir o arquivo. Código: {result}");
        }

        opened = true;

        string command = loop
            ? $"play {Alias} repeat"
            : $"play {Alias}";

        result = mciSendString(
            command,
            null,
            0,
            IntPtr.Zero);

        if (result != 0)
        {
            Stop();

            throw new InvalidOperationException(
                $"MCI não conseguiu reproduzir o arquivo. Código: {result}");
        }
    }

    public void Stop()
    {
        if (!opened)
        {
            return;
        }

        mciSendString(
            $"stop {Alias}",
            null,
            0,
            IntPtr.Zero);

        mciSendString(
            $"close {Alias}",
            null,
            0,
            IntPtr.Zero);

        opened = false;
    }

    public void Dispose()
    {
        Stop();
    }
}