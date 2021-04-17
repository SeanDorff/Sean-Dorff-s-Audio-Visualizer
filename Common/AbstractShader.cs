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

        private readonly int[] arrayBufferHandle;

        private readonly int[] elementBufferHandle;
        private readonly int[] vertexBufferHandle;
        private readonly int[] vertexArrayHandle;
        private readonly SVertexesIndexesPrimitive[] vertexesAndIndexes;

        private SBufferMapping[] bufferMappings;
        private int internalVBONumber = 0;
        private int internalVANumber = 0;

        protected int ShaderProgramHandle { get => shaderProgramHandle; }
        protected Dictionary<string, int> UniformLocations { get => uniformLocations; }

        /// <summary>
        /// The current <see cref="VertexArrayHandle"/>, select by <see cref="CurrentBuffer"/>.
        /// </summary>
        protected int VertexArrayHandle { get => vertexArrayHandle[internalVBONumber]; set => vertexArrayHandle[internalVBONumber] = value; }
        /// <summary>
        /// The current <see cref="VertexBufferHandle"/>, selected by <see cref="CurrentBuffer"/>.
        /// </summary>
        protected int VertexBufferHandle { get => vertexBufferHandle[internalVBONumber]; set => vertexBufferHandle[internalVBONumber] = value; }
        /// <summary>
        /// The current <see cref="ElementBufferHandle"/>, selected by <see cref="CurrentBuffer"/>.
        /// </summary>
        protected int ElementBufferHandle { get => elementBufferHandle[internalVBONumber]; set => elementBufferHandle[internalVBONumber] = value; }
        protected int ArrayBufferHandle { get => arrayBufferHandle[internalVANumber]; set => arrayBufferHandle[internalVANumber] = value; }

        public float[] Vertexes { get => vertexesAndIndexes[internalVBONumber].Vertexes; set => vertexesAndIndexes[internalVBONumber].Vertexes = value; }
        public uint[] Indexes { get => vertexesAndIndexes[internalVBONumber].Indexes; set => vertexesAndIndexes[internalVBONumber].Indexes = value; }

        public int CurrentBuffer { set => SetCurrentBuffer(value); }
        public AbstractShader(int shaderProgramHandle, Dictionary<string, int> uniformLocations, Dictionary<int, EBufferTypes> bufferTypes)
        {
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
            {
                bufferMappings = new SBufferMapping[bufferTypes.Count];

                this.shaderProgramHandle = shaderProgramHandle;
                this.uniformLocations = uniformLocations;

                int vertexArrayObjectBufferCount = 0;
                int arrayBufferCount = 0;

                foreach (KeyValuePair<int, EBufferTypes> bufferType in bufferTypes)
                {
                    switch (bufferType.Value)
                    {
                        case EBufferTypes.ArrayBuffer:
                            bufferMappings[internalVBONumber++] = new SBufferMapping
                            {
                                BufferNumber = bufferType.Key,
                                BufferType = bufferType.Value,
                                InternalBufferNumber = arrayBufferCount++
                            };
                            break;
                        default: // EBufferTypes.VertexArrayObject
                            bufferMappings[internalVBONumber++] = new SBufferMapping
                            {
                                BufferNumber = bufferType.Key,
                                BufferType = bufferType.Value,
                                InternalBufferNumber = vertexArrayObjectBufferCount++
                            };
                            break;
                    }
                }

                arrayBufferHandle = new int[arrayBufferCount];

                vertexArrayHandle = new int[vertexArrayObjectBufferCount];
                vertexBufferHandle = new int[vertexArrayObjectBufferCount];
                elementBufferHandle = new int[vertexArrayObjectBufferCount];
                vertexesAndIndexes = new SVertexesIndexesPrimitive[vertexArrayObjectBufferCount];

                for (int i = 0; i < bufferMappings.Length; i++)
                {
                    switch (bufferMappings[i].BufferType)
                    {
                        case EBufferTypes.ArrayBuffer:
                            internalVANumber = bufferMappings[i].InternalBufferNumber;
                            ArrayBufferHandle = GL.GenVertexArray();
                            break;
                        default: // EBufferTypes.VertexArrayObject
                            internalVBONumber = bufferMappings[i].InternalBufferNumber;
                            VertexArrayHandle = GL.GenVertexArray();
                            GL.BindVertexArray(VertexArrayHandle);

                            VertexBufferHandle = GL.GenBuffer();
                            ElementBufferHandle = GL.GenBuffer();
                            break;
                    }
                }

                internalVBONumber = 0;
            }
        }

        public void DrawElements(PrimitiveType primitiveType) => GL.DrawElements(primitiveType, Indexes.Length, DrawElementsType.UnsignedInt, 0);
        public void DrawArrays(PrimitiveType primitiveType, int length) => GL.DrawArrays(primitiveType, 0, length);

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

        private void SetCurrentBuffer(int bufferNumber)
        {
            switch (bufferMappings[bufferNumber].BufferType)
            {
                case EBufferTypes.ArrayBuffer:
                    internalVANumber = bufferMappings[bufferNumber].InternalBufferNumber;
                    break;
                default: // EBufferTypes.VertexArrayObject
                    internalVBONumber = bufferMappings[bufferNumber].InternalBufferNumber;
                    break;
            }
        }

        internal struct SVertexesIndexesPrimitive
        {
            internal float[] Vertexes;
            internal uint[] Indexes;
        }

        internal struct SBufferMapping
        {
            internal int BufferNumber;
            internal EBufferTypes BufferType;
            internal int InternalBufferNumber;
        }
    }
}
