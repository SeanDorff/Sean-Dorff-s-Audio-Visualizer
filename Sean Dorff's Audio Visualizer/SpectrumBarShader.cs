using Common;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using System.Reflection;

namespace Sean_Dorff_s_Audio_Visualizer
{
    internal class SpectrumBarShader : AbstractShader
    {
        public float[] SpectrumBarVertexes;
        public uint[] SpectrumBarVertexIndexes;

        public SpectrumBarShader(uint spectrumBarCount, uint generationsPerShader)
        {
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
            {
                SpectrumBarVertexes = new float[spectrumBarCount * ((4 * 3) + (4 * 4)) * generationsPerShader];
                SpectrumBarVertexIndexes = new uint[2 * 3 * spectrumBarCount * generationsPerShader];

                VertexArrayHandle = GL.GenVertexArray();
                GL.BindVertexArray(VertexArrayHandle);

                VertexBufferHandle = GL.GenBuffer();
                ElementBufferHandle = GL.GenBuffer();

                SendSpectrumBarData();

                Shader = new Shader("Shaders/spectrumBar.vert", "Shaders/spectrumBar.frag");
                Shader.Use();

                SetVertexAttribPointerAndArray("aPosition", 3, 7 * sizeof(float), 0);
                SetVertexAttribPointerAndArray("aColor", 4, 7 * sizeof(float), 3 * sizeof(float));
            }
        }

        public void SetModelViewProjection(Camera camera)
        {
            Shader.SetMatrix4("model", Matrix4.Identity);
            Shader.SetMatrix4("view", camera.GetViewMatrix());
            Shader.SetMatrix4("projection", camera.GetProjectionMatrix());
        }

        public void Use() => Shader.Use();

        public void BindVertexArray() => GL.BindVertexArray(VertexArrayHandle);

        public void DrawElements() => GL.DrawElements(PrimitiveType.Triangles, SpectrumBarVertexIndexes.Length, DrawElementsType.UnsignedInt, 0);

        public void SetFloat(string name, float value) => Shader.SetFloat(name, value);

        public void Unload()
        {
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, VertexArrayHandle);
                GL.BindVertexArray(VertexArrayHandle);
                GL.UseProgram(Shader.Handle);

                GL.DeleteBuffer(VertexBufferHandle);
                GL.DeleteVertexArray(VertexArrayHandle);

                GL.DeleteProgram(Shader.Handle);
            }
        }

        private void SetVertexAttribPointerAndArray(string attribute, int size, int stride, int offset)
        {
            int location = Shader.GetAttribLocation(attribute);
            GL.VertexAttribPointer(location, size, VertexAttribPointerType.Float, false, stride, offset);
            GL.EnableVertexAttribArray(location);
        }

        public void SendSpectrumBarData()
        {
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferHandle);
                GL.BufferData(BufferTarget.ArrayBuffer, SpectrumBarVertexes.Length * sizeof(float), SpectrumBarVertexes, BufferUsageHint.StreamDraw);

                GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferHandle);
                GL.BufferData(BufferTarget.ElementArrayBuffer, SpectrumBarVertexIndexes.Length * sizeof(uint), SpectrumBarVertexIndexes, BufferUsageHint.StreamDraw);
            }
        }
    }
}
