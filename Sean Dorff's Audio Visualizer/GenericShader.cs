using Common;

using OpenTK.Graphics.OpenGL4;

using System.Reflection;

namespace Sean_Dorff_s_Audio_Visualizer
{
    internal class GenericShader : AbstractShader
    {
        public GenericShader(uint vertexArrayLength, uint indexArrayLength) : base("Shaders/shader.vert", "Shaders/shader.frag", vertexArrayLength, indexArrayLength)
        {
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
            {
                SetVertexAttribPointerAndArrays();
            }
        }

        public void DrawPointElements() => GL.DrawElements(PrimitiveType.Points, Indexes.Length, DrawElementsType.UnsignedInt, 0);
        public void DrawTriangleElements() => GL.DrawElements(PrimitiveType.Triangles, Indexes.Length, DrawElementsType.UnsignedInt, 0);

        public void SetVertexAttribPointerAndArrays()
        {
            const int size = 4;
            const int stride = 8 * sizeof(float);
            const int colorOffset = size * sizeof(float);
            BindVertexArray();
            SetVertexAttribPointerAndArray("aPosition", size, stride, 0);
            SetVertexAttribPointerAndArray("aColor", size, stride, colorOffset);
        }
    }
}
