using Common;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using System.Reflection;

namespace Sean_Dorff_s_Audio_Visualizer
{
    internal class StarShader : AbstractShader
    {
        public float[] starVertexes;
        public uint[] starVertexIndexes;

        public StarShader(uint starCount)
        {
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
            {
                starVertexes = new float[starCount * 8]; // xyz, generation, RGBA
                starVertexIndexes = new uint[starCount];

                VertexArrayHandle = GL.GenVertexArray();
                GL.BindVertexArray(VertexArrayHandle);

                VertexBufferHandle = GL.GenBuffer();
                ElementBufferHandle = GL.GenBuffer();

                SendStarData();

                Shader = new Shader("Shaders/stars.vert", "Shaders/stars.frag");
                Shader.Use();

                SetVertexAttribPointerAndArray("aPosition", 4, 8 * sizeof(float), 0);
                SetVertexAttribPointerAndArray("aColor", 4, 8 * sizeof(float), 4 * sizeof(float));
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

        public void DrawElements() => GL.DrawElements(PrimitiveType.Points, starVertexIndexes.Length, DrawElementsType.UnsignedInt, 0);

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

        public void SendStarData()
        {
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferHandle);
                GL.BufferData(BufferTarget.ArrayBuffer, starVertexes.Length * sizeof(float), starVertexes, BufferUsageHint.StreamDraw);

                GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferHandle);
                GL.BufferData(BufferTarget.ElementArrayBuffer, starVertexIndexes.Length * sizeof(uint), starVertexIndexes, BufferUsageHint.StreamDraw);
            }
        }
    }
}
