using System.Text;

namespace GoQuake2.Q2File;

public static class BspLoader
{
    private const int LumpEntities = 0;
    private const int LumpPlanes = 1;
    private const int LumpVertices = 2;
    private const int LumpVisibility = 3;
    private const int LumpBspNodes = 4;
    private const int LumpTexInfos = 5;
    private const int LumpFaces = 6;
    private const int LumpLightmaps = 7;
    private const int LumpBspLeaves = 8;
    private const int LumpLeafFaces = 9;
    private const int LumpLeafBrushes = 10;
    private const int LumpEdges = 11;
    private const int LumpFaceEdges = 12;
    private const int LumpBrushes = 14;
    private const int LumpBrushSides = 15;

    public static MapData Load(Stream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.ASCII, true);
        stream.Position = 0;

        if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "IBSP")
        {
            throw new InvalidDataException("BSP Header: Wrong magic");
        }

        uint version = reader.ReadUInt32();
        if (version != 38)
        {
            throw new InvalidDataException($"BSP Header: Wrong version {version}");
        }

        var lumps = new Lump[19];
        for (int i = 0; i < lumps.Length; i++)
        {
            lumps[i] = new Lump(reader.ReadUInt32(), reader.ReadUInt32());
        }

        Console.WriteLine("Header total lumps: 19");

        var map = new MapData();

        map.EntityText = Encoding.ASCII.GetString(ReadBytes(stream, lumps[LumpEntities], "Entities"));
        map.Entities = MapEntityParser.Parse(map.EntityText);

        map.Vertices = ReadArray(
            stream,
            lumps[LumpVertices],
            12,
            r => new Vertex(r.ReadSingle(), r.ReadSingle(), r.ReadSingle()),
            "Vertex");

        map.Edges = ReadArray(
            stream,
            lumps[LumpEdges],
            4,
            r => new Edge(r.ReadUInt16(), r.ReadUInt16()),
            "Edge");

        map.Faces = ReadArray(
            stream,
            lumps[LumpFaces],
            20,
            r => new Face(
                r.ReadUInt16(),
                r.ReadUInt16(),
                r.ReadUInt32(),
                r.ReadUInt16(),
                r.ReadUInt16(),
                r.ReadBytes(4),
                r.ReadUInt32()),
            "Face");

        map.FaceEdges = ReadArray(
            stream,
            lumps[LumpFaceEdges],
            4,
            r => new FaceEdge(r.ReadInt32()),
            "Face edge");

        map.TexInfos = ReadArray(
            stream,
            lumps[LumpTexInfos],
            76,
            r =>
            {
                var textureInfo = new TexInfo();

                for (int i = 0; i < 3; i++)
                {
                    textureInfo.UAxis[i] = r.ReadSingle();
                }

                textureInfo.UOffset = r.ReadSingle();

                for (int i = 0; i < 3; i++)
                {
                    textureInfo.VAxis[i] = r.ReadSingle();
                }

                textureInfo.VOffset = r.ReadSingle();
                textureInfo.Flags = r.ReadUInt32();
                textureInfo.Value = r.ReadUInt32();
                textureInfo.TextureName = PakLoader.ReadFixed(r, 32);
                textureInfo.NextTexInfo = r.ReadInt32();
                return textureInfo;
            },
            "Tex info");

        int nextTextureId = 0;
        foreach (var textureInfo in map.TexInfos)
        {
            if (!map.TextureIds.ContainsKey(textureInfo.TextureName))
            {
                map.TextureIds[textureInfo.TextureName] = nextTextureId++;
            }
        }

        map.LightmapData = ReadBytes(stream, lumps[LumpLightmaps], "Lightmap data");

        map.Nodes = ReadArray(
            stream,
            lumps[LumpBspNodes],
            28,
            r =>
            {
                var node = new BspNode
                {
                    Plane = r.ReadUInt32(),
                    FrontChild = r.ReadInt32(),
                    BackChild = r.ReadInt32()
                };

                for (int i = 0; i < 3; i++) node.BBoxMin[i] = r.ReadInt16();
                for (int i = 0; i < 3; i++) node.BBoxMax[i] = r.ReadInt16();
                node.FirstFace = r.ReadUInt16();
                node.NumFaces = r.ReadUInt16();
                return node;
            },
            "BSP Node");

        map.Planes = ReadArray(
            stream,
            lumps[LumpPlanes],
            20,
            r =>
            {
                var plane = new Plane();
                for (int i = 0; i < 3; i++) plane.Normal[i] = r.ReadSingle();
                plane.Distance = r.ReadSingle();
                plane.Type = r.ReadUInt32();
                return plane;
            },
            "BSP Plane");

        map.BspLeaves = ReadArray(
            stream,
            lumps[LumpBspLeaves],
            28,
            r =>
            {
                var leaf = new BspLeaf
                {
                    BrushOr = r.ReadUInt32(),
                    Cluster = r.ReadUInt16(),
                    Area = r.ReadUInt16()
                };

                for (int i = 0; i < 3; i++) leaf.BBoxMin[i] = r.ReadInt16();
                for (int i = 0; i < 3; i++) leaf.BBoxMax[i] = r.ReadInt16();
                leaf.FirstLeafFace = r.ReadUInt16();
                leaf.NumLeafFaces = r.ReadUInt16();
                leaf.FirstLeafBrush = r.ReadUInt16();
                leaf.NumLeafBrushes = r.ReadUInt16();
                return leaf;
            },
            "BSP Leaf");

        map.LeafFaces = ReadArray(stream, lumps[LumpLeafFaces], 2, r => r.ReadInt16(), "Leaf face");
        map.LeafBrushes = ReadArray(stream, lumps[LumpLeafBrushes], 2, r => r.ReadUInt16(), "Leaf brush");
        map.Brushes = ReadArray(
            stream,
            lumps[LumpBrushes],
            12,
            r => new Brush(r.ReadUInt32(), r.ReadUInt32(), r.ReadUInt32()),
            "Brush");
        map.BrushSides = ReadArray(
            stream,
            lumps[LumpBrushSides],
            4,
            r => new BrushSide(r.ReadUInt16(), r.ReadInt16()),
            "Brush side");

        map.VisibilityData = ReadBytes(stream, lumps[LumpVisibility], "Visibility data");

        using (var visibilityReader = new BinaryReader(new MemoryStream(map.VisibilityData)))
        {
            uint clusterCount = visibilityReader.ReadUInt32();
            Console.WriteLine($"Visibility offset cluster count: {clusterCount}");
            map.VisibilityOffsets = new VisibilityOffset[clusterCount];

            for (int i = 0; i < clusterCount; i++)
            {
                map.VisibilityOffsets[i] = new VisibilityOffset(
                    visibilityReader.ReadUInt32(),
                    visibilityReader.ReadUInt32());
            }
        }

        return map;
    }

    private static T[] ReadArray<T>(
        Stream stream,
        Lump lump,
        int itemSize,
        Func<BinaryReader, T> read,
        string label)
    {
        int count = (int)lump.Length / itemSize;
        Console.WriteLine($"{label} count: {count}");
        stream.Position = lump.Offset;

        using var reader = new BinaryReader(stream, Encoding.ASCII, true);
        var values = new T[count];

        for (int i = 0; i < count; i++)
        {
            values[i] = read(reader);
        }

        return values;
    }

    private static byte[] ReadBytes(Stream stream, Lump lump, string label)
    {
        Console.WriteLine($"{label} count: {lump.Length}");
        stream.Position = lump.Offset;

        var data = new byte[checked((int)lump.Length)];
        int totalRead = 0;

        while (totalRead < data.Length)
        {
            int read = stream.Read(data, totalRead, data.Length - totalRead);
            if (read == 0)
            {
                throw new EndOfStreamException();
            }

            totalRead += read;
        }

        return data;
    }
}
