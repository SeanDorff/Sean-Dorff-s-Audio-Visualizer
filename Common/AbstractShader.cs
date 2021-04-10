using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using System.Reflection;

namespace Common
{
    public abstract class AbstractShader
    {
        private const int BUFFER_COUNT = 1;
        private readonly int[] elementBufferHandle = new int[BUFFER_COUNT];
        private readonly int[] vertexBufferHandle = new int[BUFFER_COUNT];
        private readonly int[] vertexArrayHandle = new int[BUFFER_COUNT];
        private readonly SVertexesAndIndexes[] vertexesAndIndexes = new SVertexesAndIndexes[BUFFER_COUNT];
        private readonly int[] vertexesCount = new int[BUFFER_COUNT];
        private readonly int[] indexesCount = new int[BUFFER_COUNT];
        private Shader shader;
        private int currentBuffer = 0;

        /// <summary>
        /// The current <see cref="ElementBufferHandle"/>, selected by <see cref="CurrentBuffer"/>.
        /// </summary>
        public int ElementBufferHandle { get => elementBufferHandle[currentBuffer]; set => elementBufferHandle[currentBuffer] = value; }
        /// <summary>
        /// The current <see cref="VertexBufferHandle"/>, selected by <see cref="CurrentBuffer"/>.
        /// </summary>
        public int VertexBufferHandle { get => vertexBufferHandle[currentBuffer]; set => vertexBufferHandle[currentBuffer] = value; }
        /// <summary>
        /// The current <see cref="VertexArrayHandle"/>, select by <see cref="CurrentBuffer"/>.
        /// </summary>
        public int VertexArrayHandle { get => vertexArrayHandle[currentBuffer]; set => vertexArrayHandle[currentBuffer] = value; }
        /// <summary>
        /// The current buffer number.
        /// </summary>
        public int CurrentBuffer { get => currentBuffer; set => currentBuffer = (value == BUFFER_COUNT ? 0 : value); }
        public float[] Vertexes { get => vertexesAndIndexes[currentBuffer].Vertexes; set => vertexesAndIndexes[currentBuffer].Vertexes = value; }
        public uint[] Indexes { get => vertexesAndIndexes[currentBuffer].Indexes; set => vertexesAndIndexes[currentBuffer].Indexes = value; }
        public int VertexesCount { get => vertexesCount[currentBuffer]; set => vertexesCount[currentBuffer] = value; }
        public int IndexesCount { get => indexesCount[currentBuffer]; set => indexesCount[currentBuffer] = value; }
        public Shader Shader { get => shader; set => shader = value; }

        public AbstractShader(string vertexShaderFile, string fragmentShaderFile, uint vertexArrayLength, uint indexArrayLength)
        {
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
            {
                Shader = new Shader(vertexShaderFile, fragmentShaderFile);
                do
                {
                    VertexArrayHandle = GL.GenVertexArray();
                    GL.BindVertexArray(VertexArrayHandle);

                    VertexBufferHandle = GL.GenBuffer();
                    ElementBufferHandle = GL.GenBuffer();

                    Vertexes = new float[vertexArrayLength];
                    Indexes = new uint[indexArrayLength];

                    currentBuffer++;
                } while (currentBuffer != BUFFER_COUNT);
                currentBuffer = 0;
            }
        }

        public void Unload()
        {
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
            {
                currentBuffer = 0;
                do
                {
                    GL.BindBuffer(BufferTarget.ArrayBuffer, VertexArrayHandle);
                    GL.BindVertexArray(VertexArrayHandle);
                    GL.UseProgram(Shader.Handle);

                    GL.DeleteBuffer(VertexBufferHandle);
                    GL.DeleteVertexArray(VertexArrayHandle);
                    currentBuffer++;
                }
                while (currentBuffer != BUFFER_COUNT);

                GL.DeleteProgram(Shader.Handle);
            }
        }

        public void Use() => Shader.Use();

        public void BindVertexArray() => GL.BindVertexArray(VertexArrayHandle);

        public void SetFloat(string name, float value) => Shader.SetFloat(name, value);
        public void SetVector3(string name, Vector3 value) => Shader.SetVector3(name, value);
        public void SetInt(string name, int value) => Shader.SetInt(name, value);

        public void SetModelViewProjection(Camera camera)
        {
            Shader.SetMatrix4("modelViewProjection", Matrix4.Identity * camera.GetViewMatrix() * camera.GetProjectionMatrix());
        }

        public void SetVertexAttribPointerAndArray(string attribute, int size, int stride, int offset)
        {
            int location = Shader.GetAttribLocation(attribute);
            GL.EnableVertexAttribArray(location);
            GL.VertexAttribPointer(location, size, VertexAttribPointerType.Float, false, stride, offset);
        }

        public void SendData()
        {
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferHandle);
                GL.BufferData(BufferTarget.ArrayBuffer, VertexesCount * sizeof(float), Vertexes, BufferUsageHint.DynamicDraw);

                GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferHandle);
                GL.BufferData(BufferTarget.ElementArrayBuffer, IndexesCount * sizeof(uint), Indexes, BufferUsageHint.DynamicDraw);
            }
        }

        private struct SVertexesAndIndexes
        {
            internal float[] Vertexes;
            internal uint[] Indexes;
        }
    }
}