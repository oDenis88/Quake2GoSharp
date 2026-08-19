using GoQuake2.Q2File;

namespace GoQuake2;

/// <summary>
/// API de dados reutilizavel por qualquer host (WinForms, WPF, console etc.).
/// Nao possui dependencia de System.Windows.Forms nem cria janelas.
/// </summary>
public sealed class Quake2ViewerService
{
    private Dictionary<string, PakFile> files = new(StringComparer.OrdinalIgnoreCase);
    private string[] maps = [];

    public string? PakPath { get; private set; }
    public bool IsPakLoaded => PakPath is not null;
    public IReadOnlyList<string> Maps => maps;

    public void LoadPak(string pakPath)
    {
        if (string.IsNullOrWhiteSpace(pakPath))
        {
            throw new ArgumentException("PAK path cannot be empty.", nameof(pakPath));
        }

        string fullPath = Path.GetFullPath(pakPath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Quake II PAK file was not found.", fullPath);
        }

        using var stream = File.OpenRead(fullPath);
        files = PakLoader.LoadQ2Pak(stream);
        PakPath = fullPath;

        maps = files.Keys
            .Where(name => name.StartsWith("maps/", StringComparison.OrdinalIgnoreCase))
            .Where(name => name.EndsWith(".bsp", StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public MapData LoadMap(string mapName)
    {
        EnsurePakLoaded();
        string normalized = EnsureMapExists(mapName);

        using var stream = File.OpenRead(PakPath!);
        return PakLoader.LoadQ2BspFromPak(stream, files, normalized);
    }

    internal (byte[] Image, WalHeader Header) LoadWal(string walName)
    {
        EnsurePakLoaded();

        using var stream = File.OpenRead(PakPath!);
        return PakLoader.LoadQ2WalFromPak(stream, files, walName);
    }

    public byte[] LoadFileBytes(string pakFileName)
    {
        EnsurePakLoaded();

        if (string.IsNullOrWhiteSpace(pakFileName))
        {
            throw new ArgumentException("PAK file name cannot be empty.", nameof(pakFileName));
        }

        string normalized = pakFileName.Replace('\\', '/').TrimStart('/');

        if (!files.TryGetValue(normalized, out PakFile file))
        {
            throw new FileNotFoundException(
                $"File '{normalized}' was not found in the loaded PAK.");
        }

        using var stream = File.OpenRead(PakPath!);
        stream.Position = file.Offset;

        byte[] data = new byte[checked((int)file.Length)];
        stream.ReadExactly(data);
        return data;
    }

    public bool TryLoadFileBytes(string pakFileName, out byte[] data)
    {
        try
        {
            data = LoadFileBytes(pakFileName);
            return true;
        }
        catch (FileNotFoundException)
        {
            data = [];
            return false;
        }
    }

    public string NormalizeMapName(string mapName)
    {
        if (string.IsNullOrWhiteSpace(mapName))
        {
            throw new ArgumentException("Map name cannot be empty.", nameof(mapName));
        }

        string normalized = mapName.Replace('\\', '/').Trim();

        if (!normalized.StartsWith("maps/", StringComparison.OrdinalIgnoreCase))
        {
            normalized = "maps/" + normalized;
        }

        if (!normalized.EndsWith(".bsp", StringComparison.OrdinalIgnoreCase))
        {
            normalized += ".bsp";
        }

        return normalized;
    }

    private string EnsureMapExists(string mapName)
    {
        string normalized = NormalizeMapName(mapName);

        if (!files.ContainsKey(normalized))
        {
            throw new FileNotFoundException(
                $"Map '{normalized}' was not found in the loaded PAK.");
        }

        return normalized;
    }

    private void EnsurePakLoaded()
    {
        if (!IsPakLoaded)
        {
            throw new InvalidOperationException(
                "No PAK is loaded. Call LoadPak(path) first.");
        }
    }
}
