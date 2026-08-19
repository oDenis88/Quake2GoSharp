namespace GoQuake2.Q2File;

public readonly record struct Lump(uint Offset, uint Length);
public readonly record struct Vertex(float X, float Y, float Z);
public readonly record struct Edge(ushort V1, ushort V2);
public readonly record struct Face(
    ushort Plane,
    ushort PlaneSide,
    uint FirstEdge,
    ushort NumEdges,
    ushort TextureInfo,
    byte[] LightmapStyles,
    uint LightmapOffset);
public readonly record struct FaceEdge(int EdgeIndex);
public readonly record struct VisibilityOffset(uint Pvs, uint Phs);
public readonly record struct PakFile(string Filename, uint Offset, uint Length);
public readonly record struct Brush(uint FirstSide, uint NumSides, uint Contents);
public readonly record struct BrushSide(ushort Plane, short TextureInfo);
public readonly record struct BspModel(
    float MinX, float MinY, float MinZ,
    float MaxX, float MaxY, float MaxZ,
    float OriginX, float OriginY, float OriginZ,
    int HeadNode, int FirstFace, int NumFaces);

public sealed class TexInfo
{
    public float[] UAxis = new float[3];
    public float UOffset;
    public float[] VAxis = new float[3];
    public float VOffset;
    public uint Flags;
    public uint Value;
    public string TextureName = "";
    public int NextTexInfo;
}

public sealed class BspNode
{
    public uint Plane;
    public int FrontChild;
    public int BackChild;
    public short[] BBoxMin = new short[3];
    public short[] BBoxMax = new short[3];
    public ushort FirstFace;
    public ushort NumFaces;
}

public sealed class Plane
{
    public float[] Normal = new float[3];
    public float Distance;
    public uint Type;
}

public sealed class BspLeaf
{
    public uint BrushOr;
    public ushort Cluster;
    public ushort Area;
    public short[] BBoxMin = new short[3];
    public short[] BBoxMax = new short[3];
    public ushort FirstLeafFace;
    public ushort NumLeafFaces;
    public ushort FirstLeafBrush;
    public ushort NumLeafBrushes;
}

public sealed class MapEntity
{
    public Dictionary<string, string> Properties { get; } = new(StringComparer.OrdinalIgnoreCase);

    public string ClassName => Properties.TryGetValue("classname", out var value) ? value : string.Empty;

    public bool TryGet(string key, out string value) => Properties.TryGetValue(key, out value!);
}

public sealed class MapData
{
    public string EntityText = "";
    public MapEntity[] Entities = [];
    public Vertex[] Vertices = [];
    public Edge[] Edges = [];
    public Face[] Faces = [];
    public FaceEdge[] FaceEdges = [];
    public TexInfo[] TexInfos = [];
    public Dictionary<string, int> TextureIds = new(StringComparer.OrdinalIgnoreCase);
    public byte[] LightmapData = [];
    public BspNode[] Nodes = [];
    public Plane[] Planes = [];
    public BspLeaf[] BspLeaves = [];
    public short[] LeafFaces = [];
    public ushort[] LeafBrushes = [];
    public Brush[] Brushes = [];
    public BrushSide[] BrushSides = [];
    public BspModel[] Models = [];
    public byte[] VisibilityData = [];
    public VisibilityOffset[] VisibilityOffsets = [];
}

public sealed class WalHeader
{
    public string Name = "";
    public uint Width;
    public uint Height;
    public int[] Offset = new int[4];
    public string NextName = "";
    public uint Flags;
    public uint Contents;
    public uint Value;
}
