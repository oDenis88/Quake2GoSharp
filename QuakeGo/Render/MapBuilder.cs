using GoQuake2.Q2File;

namespace GoQuake2.Render;

public static class MapBuilder
{
    const uint SurfaceSky=4;
    public static RenderMap Create(MapData m,MapTexture[] textures,int[] faceIds,TextureManager tm)
    {
        var by=new Dictionary<int,List<Surface>>();
        var lm=new MapLightmap(tm);
        foreach(int id in faceIds)
        {
            var f=m.Faces[id];
            var ti=m.TexInfos[f.TextureInfo];
            if((ti.Flags&SurfaceSky)!=0)continue;
            if(!m.TextureIds.TryGetValue(ti.TextureName,out int tid))continue;
            var mt=textures[tid];
            if(mt.Width==0||mt.Height==0)continue;
            if(!by.TryGetValue(tid,out var list))by[tid]=list=[];
            var fv=FaceVertices(m,f);
            var s=NewSurface(fv,ti,mt.Width,mt.Height);
            UpdateLightmap(s,lm,fv,ti,f.LightmapOffset,m,tm);
            list.Add(s);
        }
        lm.GenerateMipmaps(tm);
        var mts=textures.ToArray();
        var buf=new List<float>();
        foreach(int tid in by.Keys.Order())
        {
            var mt=mts[tid];
            mt.VertOffset=buf.Count/7;
            foreach(var s in by[tid])foreach(var v in s.Vertices)
            {
                buf.AddRange([v.X,v.Y,v.Z,v.TextureU,v.TextureV,v.LightU,v.LightV]);
                mt.VertCount++;
            }
            mts[tid]=mt;
        }
        return new()
        {
            MapTextures=mts,MapLightmap=lm,VertexBuffer=buf.ToArray()
        }
        ;
    }
    static Surface NewSurface(Vertex[] vs,TexInfo ti,uint tw,uint th)
    {
        var s=new Surface
        {
            TexInfo=ti,Vertices=new TexturedVertex[vs.Length]
        }
        ;
        for(int i=0; i<vs.Length; i++)
        {
            var uv=UV(vs[i],ti);
            s.Vertices[i]=new()
            {
                X=vs[i].X,Y=vs[i].Y,Z=vs[i].Z,TextureU=uv.u/tw,TextureV=uv.v/th,LightU=.999f,LightV=.999f
            }
            ;
        }
        return s;
    }
    static void UpdateLightmap(Surface s,MapLightmap lm,Vertex[] vs,TexInfo ti,uint offset,MapData m,TextureManager tm)
    {
        if(ti.Flags!=0||vs.Length==0)return;
        var d=Dimensions(vs,ti);
        if(d.w<=0||d.h<=0)return;
        var rect=MapLightmap.Allocate(lm.Root,d.w,d.h);
        if(rect is null)return;
        lm.Copy(offset,m.LightmapData,rect,d.w*d.h,tm);
        for(int i=0; i<s.Vertices.Length; i++)
        {
            var v=s.Vertices[i];
            float su=(v.X*ti.UAxis[0]+v.Y*ti.UAxis[1]+v.Z*ti.UAxis[2]+ti.UOffset)-d.minU;
            su+=(rect.X*16)+8;
            su/=MapLightmap.Size*16f;
            float tv=(v.X*ti.VAxis[0]+v.Y*ti.VAxis[1]+v.Z*ti.VAxis[2]+ti.VOffset)-d.minV;
            tv+=(rect.Y*16)+8;
            tv/=MapLightmap.Size*16f;
            v.LightU=su;
            v.LightV=tv;
            s.Vertices[i]=v;
        }
    }
    static (int w,int h,float minU,float minV) Dimensions(Vertex[] vs,TexInfo ti)
    {
        var a=UV(vs[0],ti);
        double minU=Math.Floor(a.u),maxU=minU,minV=Math.Floor(a.v),maxV=minV;
        for(int i=1; i<vs.Length; i++)
        {
            var uv=UV(vs[i],ti);
            minU=Math.Min(minU,Math.Floor(uv.u));
            maxU=Math.Max(maxU,Math.Floor(uv.u));
            minV=Math.Min(minV,Math.Floor(uv.v));
            maxV=Math.Max(maxV,Math.Floor(uv.v));
        }
        return ((int)(Math.Ceiling(maxU/16)-Math.Floor(minU/16)+1),(int)(Math.Ceiling(maxV/16)-Math.Floor(minV/16)+1),(float)Math.Floor(minU),(float)Math.Floor(minV));
    }
    static (float u,float v) UV(Vertex v,TexInfo t)
    {
        return (
            v.X*t.UAxis[0]+v.Y*t.UAxis[1]+v.Z*t.UAxis[2]+t.UOffset,
            v.X*t.VAxis[0]+v.Y*t.VAxis[1]+v.Z*t.VAxis[2]+t.VOffset);
    }
    static Vertex[] FaceVertices(MapData m,Face f)
    {
        var r=new List<Vertex>();
        if(f.NumEdges<3)return [];
        var v0=EdgeVertex(m,(int)f.FirstEdge);
        var v1=EdgeVertex(m,(int)f.FirstEdge+1);
        for(int o=2; o<f.NumEdges; o++)
        {
            var v2=EdgeVertex(m,(int)f.FirstEdge+o);
            r.Add(v0);
            r.Add(v1);
            r.Add(v2);
            v1=v2;
        }
        return r.ToArray();
    }
    static Vertex EdgeVertex(MapData m,int fe)
    {
        int e=m.FaceEdges[fe].EdgeIndex;
        return e>=0?m.Vertices[m.Edges[e].V1]:m.Vertices[m.Edges[-e].V2];
    }
}
