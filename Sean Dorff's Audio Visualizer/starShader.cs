using Common;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using System.Reflection;

namespace Sean_Dorff_s_Audio_Visualizer
{
    internal class StarShader : AbstractShader
    {
        public StarShader(uint starCount) : base(starCount * 8, starCount)
        {
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
            {
                GL.BindVertexArray(VertexArrayHandle);

                SendStarData();

                Shader = new Shader("Shaders/stars.vert", "Shaders/stars.frag");

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

        public void DrawElements() => GL.DrawElements(PrimitiveType.Points, Indexes.Length, DrawElementsType.UnsignedInt, 0);

        public void SetFloat(string name, float value) => Shader.SetFloat(name, value);

        public void SetVertexAttribPointerAndArrays()
        {
            GL.BindVertexArray(VertexArrayHandle);
            SetVertexAttribPointerAndArray("starPosition", 4, 8 * sizeof(float), 0);
            SetVertexAttribPointerAndArray("starColor", 4, 8 * sizeof(float), 4 * sizeof(float));
        }

        private void SetVertexAttribPointerAndArray(string attribute, int size, int stride, int offset)
        {
            int location = Shader.GetAttribLocation(attribute);
            GL.VertexAttribPointer(location, size, VertexAttribPointerType.Float, false, stride, offset);
            GL.EnableVertexAttribArray(location);
        }

        public void SendStarData()
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
