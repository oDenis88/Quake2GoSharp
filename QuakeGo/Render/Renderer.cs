using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace GoQuake2.Render;

public sealed class Renderer:IDisposable
{
    int vao,vbo;
    readonly Shader shader;
    public TextureManager Textures
    {
        get;
    }
    =new();
    public Renderer()
    {
        Console.WriteLine($"OpenGL version {GL.GetString(StringName.Version)}");
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha,BlendingFactor.OneMinusSrcAlpha);
        GL.Enable(EnableCap.CullFace);
        GL.CullFace(CullFaceMode.Front);
        vao=GL.GenVertexArray();
        vbo=GL.GenBuffer();
        shader=new Shader(Path.Combine(AppContext.BaseDirectory,"Shaders","goquake2.vert"),Path.Combine(AppContext.BaseDirectory,"Shaders","goquake2.frag"));
    }
    public void Prepare(Matrix4 view,Matrix4 projection)
    {
        GL.Clear(ClearBufferMask.ColorBufferBit|ClearBufferMask.DepthBufferBit);
        shader.Use();
        shader.Mat4("view",view);
        shader.Mat4("projection",projection);
    }
    public void DrawSkybox()
    {
        GL.ClearColor(.3f,.1f,.05f,1);
        GL.Clear(ClearBufferMask.ColorBufferBit);
    }
    public void Draw(RenderMap m)
    {
        if(m.MapLightmap is null||m.VertexBuffer.Length==0)return;
        GL.BindVertexArray(vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer,vbo);
        GL.BufferData(BufferTarget.ArrayBuffer,m.VertexBuffer.Length*sizeof(float),m.VertexBuffer,BufferUsageHint.StaticDraw);
        int stride=7*sizeof(float);
        GL.VertexAttribPointer(0,3,VertexAttribPointerType.Float,false,stride,0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1,2,VertexAttribPointerType.Float,false,stride,3*sizeof(float));
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(2,2,VertexAttribPointerType.Float,false,stride,5*sizeof(float));
        GL.EnableVertexAttribArray(2);
        shader.Int("diffuse",0);
        GL.ActiveTexture(TextureUnit.Texture1);
        GL.BindTexture(TextureTarget.Texture2D,m.MapLightmap.Texture);
        shader.Int("lightmap",1);
        foreach(var t in m.MapTextures)
        {
            if(t.VertCount==0)continue;
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D,t.Id);
            GL.DrawArrays(PrimitiveType.Triangles,t.VertOffset,t.VertCount);
        }
    }
    public void Resize(int w,int h)
    {
        GL.Viewport(0,0,w,h);
    }
    public void Dispose()
    {
        shader.Dispose();
        Textures.Dispose();
        GL.DeleteBuffer(vbo);
        GL.DeleteVertexArray(vao);
    }
}
