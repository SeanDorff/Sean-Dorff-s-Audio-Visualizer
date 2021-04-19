using OpenTK.Mathematics;

using System.Collections.Generic;
using System.Reflection;

namespace Common
{
    public class TextureShader : AbstractShader
    {
        public TextureShader(int shaderProgramHandle, Dictionary<string, int> uniformLocations, Dictionary<int, EBufferTypes> bufferTypes) : base(shaderProgramHandle, uniformLocations, bufferTypes)
        {

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
                const int C_Size = 12;
                const int C_Stride = 24 * sizeof(float);
                const int C_Offset = C_Size * sizeof(float);
                BindVertexArrayObject();
                //BindArrayBuffer();
                SetVertexAttribPointerAndArray("vp", C_Size, C_Stride, 0);
                SetVertexAttribPointerAndArray("vt", C_Size, C_Stride, C_Offset);
            }
        }
    }
}
