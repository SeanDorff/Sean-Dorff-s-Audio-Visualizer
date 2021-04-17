
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using System.Collections.Generic;
using System.Reflection;

namespace Common
{
    public class TextureShader : AbstractShader
    {
        public TextureShader(int shaderProgramHandle, Dictionary<string, int> uniformLocations, int bufferCount = 1) : base(shaderProgramHandle, uniformLocations, bufferCount)
        {

        }

        public void SendData()
        {
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
            {
                BindVertexArray();
                GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferHandle);
                GL.BufferData(BufferTarget.ArrayBuffer, Vertexes.Length * sizeof(float), Vertexes, BufferUsageHint.DynamicDraw);

                GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferHandle);
                GL.BufferData(BufferTarget.ElementArrayBuffer, Indexes.Length * sizeof(uint), Indexes, BufferUsageHint.DynamicDraw);
            }
        }

        public void SetModelViewProjection(Camera camera)
        {
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
                SetMatrix4("modelViewProjection", Matrix4.Identity * camera.GetViewMatrix() * camera.GetProjectionMatrix());
        }

        public void SetVertexAttribPointerAndArrays()
        {
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
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
}
