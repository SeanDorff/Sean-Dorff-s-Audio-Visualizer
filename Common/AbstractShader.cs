using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using System.Collections.Generic;
using System.Reflection;

namespace Common
{
    public abstract class AbstractShader : IShader
    {
        private readonly int shaderProgramHandle;
        private readonly Dictionary<string, int> uniformLocations;

        private readonly int bufferCount = 1;
        private readonly int[] elementBufferHandle;
        private readonly int[] vertexBufferHandle;
        private readonly int[] vertexArrayHandle;
        private readonly SVertexesIndexesPrimitive[] vertexesAndIndexes;
        private readonly PrimitiveType[] primitiveTypes;

        private int currentBuffer = 0;

        protected int ShaderProgramHandle { get => shaderProgramHandle; }
        protected Dictionary<string, int> UniformLocations { get => uniformLocations; }

        /// <summary>
        /// The current <see cref="ElementBufferHandle"/>, selected by <see cref="CurrentBuffer"/>.
        /// </summary>
        protected int ElementBufferHandle { get => elementBufferHandle[currentBuffer]; set => elementBufferHandle[currentBuffer] = value; }
        /// <summary>
        /// The current <see cref="VertexBufferHandle"/>, selected by <see cref="CurrentBuffer"/>.
        /// </summary>
        protected int VertexBufferHandle { get => vertexBufferHandle[currentBuffer]; set => vertexBufferHandle[currentBuffer] = value; }
        /// <summary>
        /// The current <see cref="VertexArrayHandle"/>, select by <see cref="CurrentBuffer"/>.
        /// </summary>
        protected int VertexArrayHandle { get => vertexArrayHandle[currentBuffer]; set => vertexArrayHandle[currentBuffer] = value; }

        public float[] Vertexes { get => vertexesAndIndexes[currentBuffer].Vertexes; set => vertexesAndIndexes[currentBuffer].Vertexes = value; }
        public uint[] Indexes { get => vertexesAndIndexes[currentBuffer].Indexes; set => vertexesAndIndexes[currentBuffer].Indexes = value; }
        public PrimitiveType PrimitiveType { get => primitiveTypes[currentBuffer]; set => primitiveTypes[currentBuffer] = value; }

        public int CurrentBuffer { set => currentBuffer = value; }
        public AbstractShader(int shaderProgramHandle, Dictionary<string, int> uniformLocations, int bufferCount = 1)
        {
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
            {
                this.shaderProgramHandle = shaderProgramHandle;
                this.uniformLocations = uniformLocations;

                this.bufferCount = bufferCount;

                elementBufferHandle = new int[bufferCount];
                vertexBufferHandle = new int[bufferCount];
                vertexArrayHandle = new int[bufferCount];
                vertexesAndIndexes = new SVertexesIndexesPrimitive[bufferCount];
                primitiveTypes = new PrimitiveType[bufferCount];

                do
                {
                    VertexArrayHandle = GL.GenVertexArray();
                    GL.BindVertexArray(VertexArrayHandle);

                    VertexBufferHandle = GL.GenBuffer();
                    ElementBufferHandle = GL.GenBuffer();

                    currentBuffer++;
                } while (currentBuffer != bufferCount);
                currentBuffer = 0;
            }
        }

        public void DrawElements() => GL.DrawElements(PrimitiveType, Indexes.Length, DrawElementsType.UnsignedInt, 0);
        public void DrawArrays(int length) => GL.DrawArrays(PrimitiveType, 0, length);

        #region Uniform setters
        /// <summary>
        /// Set a uniform float on this shader.
        /// </summary>
        /// <param name="name">The name of the uniform</param>
        /// <param name="data">The data to set</param>
        public void SetFloat(string name, float data)
        {
            Use();
            GL.Uniform1(UniformLocations[name], data);
        }

        /// <summary>
        /// Set a uniform float array on this shader.
        /// </summary>
        /// <param name="name">The name of the uniform</param>
        /// <param name="data">The data to set</param>
        public void SetFloatArray(string name, float[] data)
        {
            Use();
            unsafe
            {
                fixed (float* pointerToFirst = &data[0])
                    GL.Uniform1(UniformLocations[name], data.Length, pointerToFirst);
            }
        }

        /// <summary>
        /// Set a uniform int on this shader.
        /// </summary>
        /// <param name="name">The name of the uniform</param>
        /// <param name="data">The data to set</param>
        public void SetInt(string name, int data)
        {
            Use();
            GL.Uniform1(UniformLocations[name], data);
        }

        /// <summary>
        /// Set a uniform Matrix4 on this shader.
        /// </summary>
        /// <param name="name">The name of the uniform</param>
        /// <param name="data">The data to set</param>
        /// <remarks>
        /// The matrix is transposed before being sent to the shader.
        /// </remarks>
        public void SetMatrix4(string name, Matrix4 data)
        {
            Use();
            GL.UniformMatrix4(UniformLocations[name], true, ref data);
        }

        /// <summary>
        /// Set a uniform Vector3 on this shader.
        /// </summary>
        /// <param name="name">The name of the uniform</param>
        /// <param name="data">The data to set</param>
        public void SetVector3(string name, Vector3 data)
        {
            Use();
            GL.Uniform3(UniformLocations[name], data);
        }
        #endregion
        /// <summary>
        /// Wrapper method that enables the shader program.
        /// </summary>
        public void Use() => GL.UseProgram(ShaderProgramHandle);

        public void Unload()
        {
            GL.DeleteProgram(ShaderProgramHandle);
        }

        private int GetAttribLocation(string attribName) => GL.GetAttribLocation(ShaderProgramHandle, attribName);

        protected void SetVertexAttribPointerAndArray(string attribute, int size, int stride, int offset)
        {
            int location = GetAttribLocation(attribute);
            GL.EnableVertexAttribArray(location);
            GL.VertexAttribPointer(location, size, VertexAttribPointerType.Float, false, stride, offset);
        }

        protected void BindVertexArray() => GL.BindVertexArray(VertexArrayHandle);

        internal struct SVertexesIndexesPrimitive
        {
            internal float[] Vertexes;
            internal uint[] Indexes;
        }
    }
}
