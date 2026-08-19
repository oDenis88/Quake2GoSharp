using GoQuake2.Q2File;
using OpenTK.Graphics.OpenGL4;

namespace GoQuake2.Render;

public sealed class TextureManager:IDisposable
{
    readonly HashSet<int> textures=[];
    public int CreateTexture2D(int w,int h,PixelFormat format,byte[]? data)
    {
        if(w<=0||h<=0)return 0;
        int id=GL.GenTexture();
        textures.Add(id);
        GL.BindTexture(TextureTarget.Texture2D,id);
        GL.PixelStore(PixelStoreParameter.UnpackAlignment,1);
        GL.TexImage2D(TextureTarget.Texture2D,0,format==PixelFormat.Rgba?PixelInternalFormat.Rgba:PixelInternalFormat.Rgb,w,h,0,format,PixelType.UnsignedByte,data);
        GL.TexParameter(TextureTarget.Texture2D,TextureParameterName.TextureMagFilter,(int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D,TextureParameterName.TextureMinFilter,(int)TextureMinFilter.Linear);
        return id;
    }
    public int CreateLightmapTexture(int size)
    {
        int id=GL.GenTexture();
        textures.Add(id);
        GL.BindTexture(TextureTarget.Texture2D,id);
        GL.TexImage2D(TextureTarget.Texture2D,0,PixelInternalFormat.Rgba,size,size,0,PixelFormat.Rgba,PixelType.UnsignedByte,IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D,TextureParameterName.TextureMagFilter,(int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D,TextureParameterName.TextureMinFilter,(int)TextureMinFilter.LinearMipmapLinear);
        return id;
    }
    public int CreateWalTexture(byte[] image,WalHeader w)
    {
        return CreateTexture2D((int)w.Width,(int)w.Height,PixelFormat.Rgb,image);
    }
    public void UpdateSubImage(int id,int x,int y,int w,int h,byte[] data)
    {
        GL.BindTexture(TextureTarget.Texture2D,id);
        GL.TexSubImage2D(TextureTarget.Texture2D,0,x,y,w,h,PixelFormat.Rgba,PixelType.UnsignedByte,data);
    }
    public void GenerateMipmaps(int id)
    {
        GL.BindTexture(TextureTarget.Texture2D,id);
        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
    }
    public void Dispose()
    {
        foreach(var id in textures)GL.DeleteTexture(id);
        textures.Clear();
    }
}
