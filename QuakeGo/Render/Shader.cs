using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace GoQuake2.Render;

public sealed class Shader:IDisposable
{
    public int Program
    {
        get;
    }
    public Shader(string vert,string frag)
    {
        int vs=Compile(File.ReadAllText(vert),ShaderType.VertexShader),fs=Compile(File.ReadAllText(frag),ShaderType.FragmentShader);
        Program=GL.CreateProgram();
        GL.AttachShader(Program,vs);
        GL.AttachShader(Program,fs);
        GL.LinkProgram(Program);
        GL.GetProgram(Program,GetProgramParameterName.LinkStatus,out int ok);
        if(ok==0)throw new InvalidOperationException(GL.GetProgramInfoLog(Program));
        GL.DetachShader(Program,vs);
        GL.DetachShader(Program,fs);
        GL.DeleteShader(vs);
        GL.DeleteShader(fs);
    }
    static int Compile(string s,ShaderType t)
    {
        int id=GL.CreateShader(t);
        GL.ShaderSource(id,s);
        GL.CompileShader(id);
        GL.GetShader(id,ShaderParameter.CompileStatus,out int ok);
        if(ok==0)throw new InvalidOperationException(GL.GetShaderInfoLog(id));
        return id;
    }
    public void Use()
    {
        GL.UseProgram(Program);
    }
    public void Mat4(string n,Matrix4 m)
    {
        int l=GL.GetUniformLocation(Program,n);
        GL.UniformMatrix4(l,false,ref m);
    }
    public void Int(string n,int v)
    {
        GL.Uniform1(GL.GetUniformLocation(Program,n),v);
    }
    public void Dispose()
    {
        GL.DeleteProgram(Program);
    }
}
