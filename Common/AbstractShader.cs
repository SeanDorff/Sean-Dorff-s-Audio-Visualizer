using OpenTK.Graphics.OpenGL4;

using System.Reflection;

namespace Common
{
    public abstract class AbstractShader
    {
        private const int bufferCount = 1;
        private readonly int[] elementBufferHandle = new int[bufferCount];
        private readonly int[] vertexBufferHandle = new int[bufferCount];
        private readonly int[] vertexArrayHandle = new int[bufferCount];
        private readonly SVertexesAndIndexes[] vertexesAndIndexes = new SVertexesAndIndexes[bufferCount];
        private Shader shader;
        private int currentBuffer = 0;

        public int ElementBufferHandle { get => elementBufferHandle[currentBuffer]; set => elementBufferHandle[currentBuffer] = value; }
        public int VertexBufferHandle { get => vertexBufferHandle[currentBuffer]; set => vertexBufferHandle[currentBuffer] = value; }
        public int VertexArrayHandle { get => vertexArrayHandle[currentBuffer]; set => vertexArrayHandle[currentBuffer] = value; }
        public int CurrentBuffer { get => currentBuffer; set => currentBuffer = (value == bufferCount ? 0 : value); }
        public float[] Vertexes { get => vertexesAndIndexes[currentBuffer].Vertexes; set => vertexesAndIndexes[currentBuffer].Vertexes = value; }
        public uint[] Indexes { get => vertexesAndIndexes[currentBuffer].Indexes; set => vertexesAndIndexes[currentBuffer].Indexes = value; }
        public Shader Shader { get => shader; set => shader = value; }

        public AbstractShader(uint vertexArrayLength, uint indexArrayLength)
        {
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
            {
                do
                {
                    VertexArrayHandle = GL.GenVertexArray();
                    GL.BindVertexArray(VertexArrayHandle);

                    VertexBufferHandle = GL.GenBuffer();
                    ElementBufferHandle = GL.GenBuffer();

                    Vertexes = new float[vertexArrayLength];
                    Indexes = new uint[indexArrayLength];

                    currentBuffer++;
                } while (currentBuffer != bufferCount);
                currentBuffer = 0;
            }
        }

        public void Unload()
        {
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
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
                while (currentBuffer != bufferCount);

                GL.DeleteProgram(Shader.Handle);
            }
        }

        public struct SVertexesAndIndexes
        {
            public float[] Vertexes;
            public uint[] Indexes;
        }
    }
}