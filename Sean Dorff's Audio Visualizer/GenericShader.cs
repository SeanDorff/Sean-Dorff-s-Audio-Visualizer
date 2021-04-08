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
            BindVertexArray();
            SetVertexAttribPointerAndArray("aPosition", 4, 8 * sizeof(float), 0);
            SetVertexAttribPointerAndArray("aColor", 4, 8 * sizeof(float), 4 * sizeof(float));
        }
    }
}
