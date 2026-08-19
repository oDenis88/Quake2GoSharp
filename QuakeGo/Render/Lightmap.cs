namespace GoQuake2.Render;

public sealed class LightmapNode
{
    public int X,Y,Width,Height;
    public LightmapNode[]? Nodes;
    public bool Filled;
}
public sealed class MapLightmap
{
    public const int Size=512;
    public int Texture;
    public LightmapNode Root;
    public MapLightmap(TextureManager tm)
    {
        Texture=tm.CreateLightmapTexture(Size);
        Root=new()
        {
            X=0,Y=0,Width=Size,Height=Size
        }
        ;
    }
    public void GenerateMipmaps(TextureManager tm)
    {
        tm.GenerateMipmaps(Texture);
    }
    public void Copy(uint offset,byte[] src,LightmapNode n,int total,TextureManager tm)
    {
        var px=new byte[total*4];
        int rpos=checked((int)offset);
        for(int i=0; i<total; i++,rpos+=3)
        {
            if(rpos+2>=src.Length)break;
            int r=src[rpos]*4,g=src[rpos+1]*4,b=src[rpos+2]*4,max=Math.Max(r,Math.Max(g,b));
            if(max>255)
            {
                float t=255f/max;
                r=(int)(r*t);
                g=(int)(g*t);
                b=(int)(b*t);
            }
            int o=i*4;
            px[o]=(byte)r;
            px[o+1]=(byte)g;
            px[o+2]=(byte)b;
            px[o+3]=255;
        }
        tm.UpdateSubImage(Texture,n.X,n.Y,n.Width,n.Height,px);
    }
    public static LightmapNode? Allocate(LightmapNode n,int w,int h)
    {
        if(n.Nodes is
        {
            Length:>0
        }
        )return Allocate(n.Nodes[0],w,h)??Allocate(n.Nodes[1],w,h);
        if(n.Filled||n.Width<w||n.Height<h)return null;
        if(n.Width==w&&n.Height==h)
        {
            n.Filled=true;
            return n;
        }
        if(n.Width-w>n.Height-h)n.Nodes=[new()
        {
            X=n.X,Y=n.Y,Width=w,Height=n.Height
        }
        ,new()
        {
            X=n.X+w,Y=n.Y,Width=n.Width-w,Height=n.Height
        }
        ];
        else n.Nodes=[new()
        {
            X=n.X,Y=n.Y,Width=n.Width,Height=h
        }
        ,new()
        {
            X=n.X,Y=n.Y+h,Width=n.Width,Height=n.Height-h
        }
        ];
        return Allocate(n.Nodes[0],w,h);
    }
}
