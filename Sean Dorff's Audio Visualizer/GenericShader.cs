using Common;

using OpenTK.Graphics.OpenGL4;

using System.Reflection;

namespace Sean_Dorff_s_Audio_Visualizer
{
    internal class GenericShader : AbstractShader
    {
        public GenericShader(uint vertexArrayLength, uint indexArrayLength) : base("Shaders/shader.vert", "Shaders/shader.frag", vertexArrayLength, indexArrayLength)
        {
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
            {
                SetVertexAttribPointerAndArrays();
            }
        }

        public void DrawPointElements() => GL.DrawElements(PrimitiveType.Points, Indexes.Length, DrawElementsType.UnsignedInt, 0);
        public void DrawTriangleElements() => GL.DrawElements(PrimitiveType.Triangles, Indexes.Length, DrawElementsType.UnsignedInt, 0);

        public void SetVertexAttribPointerAndArrays()
        {
            const int C_Size = 4;
            const int C_Stride = 8 * sizeof(float);
            const int C_ColorOffset = C_Size * sizeof(float);
            BindVertexArray();
            SetVertexAttribPointerAndArray("aPosition", C_Size, C_Stride, 0);
            SetVertexAttribPointerAndArray("aColor", C_Size, C_Stride, C_ColorOffset);
        }
    }
}
