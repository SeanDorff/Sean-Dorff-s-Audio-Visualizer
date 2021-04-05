using Common;

using OpenTK.Graphics.OpenGL4;

namespace Sean_Dorff_s_Audio_Visualizer
{
    internal class SpectrumBarShader : AbstractShader
    {
        public float[] SpectrumBarVertexes;
        public uint[] SpectrumBarVertexIndexes;

        public void Unload()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, VertexArrayHandle);
            GL.BindVertexArray(VertexArrayHandle);
            GL.UseProgram(Shader.Handle);

            GL.DeleteBuffer(VertexBufferHandle);
            GL.DeleteVertexArray(VertexArrayHandle);

            GL.DeleteProgram(Shader.Handle);
        }
    }
}
