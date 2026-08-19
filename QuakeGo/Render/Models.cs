using GoQuake2.Q2File;

namespace GoQuake2.Render;

public struct MapTexture
{
    public int Id;
    public uint Width,Height;
    public int VertOffset,VertCount;
    public MapTexture(int id,uint w,uint h)
    {
        Id=id;
        Width=w;
        Height=h;
        VertOffset=VertCount=0;
    }
}
public struct TexturedVertex
{
    public float X,Y,Z,TextureU,TextureV,LightU,LightV;
}
public sealed class Surface
{
    public TexInfo TexInfo=null!;
    public TexturedVertex[] Vertices=[];
}
public sealed class RenderMap
{
    public MapTexture[] MapTextures=[];
    public MapLightmap? MapLightmap;
    public float[] VertexBuffer=[];
}
