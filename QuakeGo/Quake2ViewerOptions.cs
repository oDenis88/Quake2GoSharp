namespace GoQuake2;

public sealed class Quake2ViewerOptions
{
    public int Width { get; set; } = 1024;
    public int Height { get; set; } = 768;
    public string WindowTitle { get; set; } = "Quake II BSP Viewer";
    public bool VSync { get; set; }
}
