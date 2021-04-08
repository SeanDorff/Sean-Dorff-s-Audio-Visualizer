using Common;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using System.Reflection;

namespace Sean_Dorff_s_Audio_Visualizer
{
    internal class SpectrumBarShader : AbstractShader
    {
        public SpectrumBarShader(uint spectrumBarCount, uint generationsPerShader) : base(spectrumBarCount * ((4 * 4) + (4 * 4)) * generationsPerShader, 2 * 3 * spectrumBarCount * generationsPerShader)
        {
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
            {
                GL.BindVertexArray(VertexArrayHandle);

                SendSpectrumBarData();

                Shader = new Shader("Shaders/spectrumBar.vert", "Shaders/spectrumBar.frag");

                SetVertexAttribPointerAndArrays();
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

        public void DrawElements() => GL.DrawElements(PrimitiveType.Triangles, Indexes.Length, DrawElementsType.UnsignedInt, 0);

        public void SetFloat(string name, float value) => Shader.SetFloat(name, value);

        public void SetVertexAttribPointerAndArrays()
        {
            GL.BindVertexArray(VertexArrayHandle);
            SetVertexAttribPointerAndArray("spectrumPosition", 4, 8 * sizeof(float), 0);
            SetVertexAttribPointerAndArray("spectrumColor", 4, 8 * sizeof(float), 4 * sizeof(float));
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
                GL.BufferData(BufferTarget.ArrayBuffer, Vertexes.Length * sizeof(float), Vertexes, BufferUsageHint.StreamCopy);

                GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferHandle);
                GL.BufferData(BufferTarget.ElementArrayBuffer, Indexes.Length * sizeof(uint), Indexes, BufferUsageHint.StreamCopy);
            }
        }
    }
}
