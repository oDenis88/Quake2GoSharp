using System.Runtime.InteropServices;

namespace QuakeReader.Audio;

public sealed class WaveSoundPlayer : IDisposable
{
    private const uint SndAsync = 0x0001;
    private const uint SndNoDefault = 0x0002;
    private const uint SndMemory = 0x0004;

    [DllImport("winmm.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PlaySound(
        IntPtr pszSound,
        IntPtr hmod,
        uint fdwSound);

    private readonly byte[] waveData;
    private GCHandle pinnedData;
    private bool disposed;

    public WaveSoundPlayer(byte[] waveData)
    {
        ArgumentNullException.ThrowIfNull(waveData);

        if (waveData.Length == 0)
        {
            throw new ArgumentException("WAV data cannot be empty.", nameof(waveData));
        }

        this.waveData = waveData;
        pinnedData = GCHandle.Alloc(this.waveData, GCHandleType.Pinned);
    }

    public void Play()
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        PlaySound(
            pinnedData.AddrOfPinnedObject(),
            IntPtr.Zero,
            SndAsync | SndNoDefault | SndMemory);
    }

    public void Stop()
    {
        if (!disposed)
        {
            PlaySound(IntPtr.Zero, IntPtr.Zero, 0);
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        Stop();

        if (pinnedData.IsAllocated)
        {
            pinnedData.Free();
        }

        disposed = true;
    }
}
