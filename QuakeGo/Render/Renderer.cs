using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace GoQuake2.Render;

public sealed class Renderer : IDisposable
{
    private readonly int vao;
    private readonly int vbo;
    private readonly Shader shader;

    private readonly int blasterVao;
    private readonly int blasterVbo;
    private readonly int blasterProgram;
    private readonly int blasterViewLocation;
    private readonly int blasterProjectionLocation;
    private readonly int blasterColorLocation;

    public TextureManager Textures { get; } = new();

    public Renderer()
    {
        Console.WriteLine($"OpenGL version {GL.GetString(StringName.Version)}");

        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.Enable(EnableCap.CullFace);
        GL.CullFace(CullFaceMode.Front);

        vao = GL.GenVertexArray();
        vbo = GL.GenBuffer();

        shader = new Shader(
            Path.Combine(AppContext.BaseDirectory, "Shaders", "goquake2.vert"),
            Path.Combine(AppContext.BaseDirectory, "Shaders", "goquake2.frag"));

        blasterVao = GL.GenVertexArray();
        blasterVbo = GL.GenBuffer();
        blasterProgram = CreateBlasterProgram();

        blasterViewLocation = GL.GetUniformLocation(blasterProgram, "view");
        blasterProjectionLocation = GL.GetUniformLocation(blasterProgram, "projection");
        blasterColorLocation = GL.GetUniformLocation(blasterProgram, "color");

        GL.BindVertexArray(blasterVao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, blasterVbo);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);
    }

    public void Prepare(Matrix4 view, Matrix4 projection)
    {
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        shader.Use();
        shader.Mat4("view", view);
        shader.Mat4("projection", projection);
    }

    public void DrawSkybox()
    {
        GL.ClearColor(.3f, .1f, .05f, 1f);
        GL.Clear(ClearBufferMask.ColorBufferBit);
    }

    public void Draw(RenderMap map)
    {
        if (map.MapLightmap is null || map.VertexBuffer.Length == 0)
        {
            return;
        }

        GL.BindVertexArray(vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(
            BufferTarget.ArrayBuffer,
            map.VertexBuffer.Length * sizeof(float),
            map.VertexBuffer,
            BufferUsageHint.StaticDraw);

        int stride = 7 * sizeof(float);

        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
        GL.EnableVertexAttribArray(0);

        GL.VertexAttribPointer(
            1,
            2,
            VertexAttribPointerType.Float,
            false,
            stride,
            3 * sizeof(float));
        GL.EnableVertexAttribArray(1);

        GL.VertexAttribPointer(
            2,
            2,
            VertexAttribPointerType.Float,
            false,
            stride,
            5 * sizeof(float));
        GL.EnableVertexAttribArray(2);

        shader.Int("diffuse", 0);

        GL.ActiveTexture(TextureUnit.Texture1);
        GL.BindTexture(TextureTarget.Texture2D, map.MapLightmap.Texture);
        shader.Int("lightmap", 1);

        foreach (var texture in map.MapTextures)
        {
            if (texture.VertCount == 0)
            {
                continue;
            }

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, texture.Id);
            GL.DrawArrays(
                PrimitiveType.Triangles,
                texture.VertOffset,
                texture.VertCount);
        }
    }

    /// <summary>
    /// Desenha um bolt simples de blaster. Usa shader proprio para nao depender
    /// das texturas/lightmaps do mapa.
    /// </summary>
    public void DrawBlaster(
        Vector3 start,
        Vector3 end,
        Matrix4 view,
        Matrix4 projection)
    {
        float[] vertices =
        [
            start.X, start.Y, start.Z,
            end.X, end.Y, end.Z
        ];

        GL.UseProgram(blasterProgram);
        GL.UniformMatrix4(blasterViewLocation, false, ref view);
        GL.UniformMatrix4(blasterProjectionLocation, false, ref projection);

        // Amarelo/laranja, lembrando o blaster do Quake II.
        GL.Uniform4(blasterColorLocation, 1.0f, 0.65f, 0.08f, 1.0f);

        GL.BindVertexArray(blasterVao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, blasterVbo);
        GL.BufferData(
            BufferTarget.ArrayBuffer,
            vertices.Length * sizeof(float),
            vertices,
            BufferUsageHint.DynamicDraw);

        bool cullWasEnabled = GL.IsEnabled(EnableCap.CullFace);
        if (cullWasEnabled)
        {
            GL.Disable(EnableCap.CullFace);
        }

        GL.LineWidth(7f);
        GL.DrawArrays(PrimitiveType.Lines, 0, 2);
        GL.LineWidth(1f);

        GL.PointSize(12f);
        GL.DrawArrays(PrimitiveType.Points, 1, 1);
        GL.PointSize(1f);

        if (cullWasEnabled)
        {
            GL.Enable(EnableCap.CullFace);
        }
    }

    public void Resize(int width, int height)
    {
        GL.Viewport(0, 0, width, height);
    }

    public void Dispose()
    {
        shader.Dispose();
        Textures.Dispose();

        GL.DeleteProgram(blasterProgram);
        GL.DeleteBuffer(blasterVbo);
        GL.DeleteVertexArray(blasterVao);

        GL.DeleteBuffer(vbo);
        GL.DeleteVertexArray(vao);
    }

    private static int CreateBlasterProgram()
    {
        const string vertexSource = """
            #version 330 core
            layout (location = 0) in vec3 position;
            uniform mat4 view;
            uniform mat4 projection;

            void main()
            {
                gl_Position = projection * view * vec4(position, 1.0);
            }
            """;

        const string fragmentSource = """
            #version 330 core
            uniform vec4 color;
            out vec4 fragColor;

            void main()
            {
                fragColor = color;
            }
            """;

        int vertexShader = CompileShader(vertexSource, ShaderType.VertexShader);
        int fragmentShader = CompileShader(fragmentSource, ShaderType.FragmentShader);
        int program = GL.CreateProgram();

        GL.AttachShader(program, vertexShader);
        GL.AttachShader(program, fragmentShader);
        GL.LinkProgram(program);

        GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int linked);
        if (linked == 0)
        {
            string log = GL.GetProgramInfoLog(program);

            GL.DeleteProgram(program);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

            throw new InvalidOperationException($"Erro ao linkar shader do blaster: {log}");
        }

        GL.DetachShader(program, vertexShader);
        GL.DetachShader(program, fragmentShader);
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);

        return program;
    }

    private static int CompileShader(string source, ShaderType type)
    {
        int shaderId = GL.CreateShader(type);
        GL.ShaderSource(shaderId, source);
        GL.CompileShader(shaderId);

        GL.GetShader(shaderId, ShaderParameter.CompileStatus, out int compiled);
        if (compiled == 0)
        {
            string log = GL.GetShaderInfoLog(shaderId);
            GL.DeleteShader(shaderId);
            throw new InvalidOperationException($"Erro ao compilar shader do blaster: {log}");
        }

        return shaderId;
    }
}
