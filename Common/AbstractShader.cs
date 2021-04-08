using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

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
        private readonly int[] vertexesCount = new int[bufferCount];
        private readonly int[] indexesCount = new int[bufferCount];
        private Shader shader;
        private int currentBuffer = 0;

        public int ElementBufferHandle { get => elementBufferHandle[currentBuffer]; set => elementBufferHandle[currentBuffer] = value; }
        public int VertexBufferHandle { get => vertexBufferHandle[currentBuffer]; set => vertexBufferHandle[currentBuffer] = value; }
        public int VertexArrayHandle { get => vertexArrayHandle[currentBuffer]; set => vertexArrayHandle[currentBuffer] = value; }
        public int CurrentBuffer { get => currentBuffer; set => currentBuffer = (value == bufferCount ? 0 : value); }
        public float[] Vertexes { get => vertexesAndIndexes[currentBuffer].Vertexes; set => vertexesAndIndexes[currentBuffer].Vertexes = value; }
        public uint[] Indexes { get => vertexesAndIndexes[currentBuffer].Indexes; set => vertexesAndIndexes[currentBuffer].Indexes = value; }
        public int VertexesCount { get => vertexesCount[currentBuffer]; set => vertexesCount[currentBuffer] = value; }
        public int IndexesCount { get => indexesCount[currentBuffer]; set => indexesCount[currentBuffer] = value; }
        public Shader Shader { get => shader; set => shader = value; }

        public AbstractShader(string vertexShaderFile, string fragmentShaderFile, uint vertexArrayLength, uint indexArrayLength)
        {
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
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

        public void Use() => Shader.Use();

        public void BindVertexArray() => GL.BindVertexArray(VertexArrayHandle);

        public void SetFloat(string name, float value) => Shader.SetFloat(name, value);

        public void SetModelViewProjection(Camera camera)
        {
            Shader.SetMatrix4("model", Matrix4.Identity);
            Shader.SetMatrix4("view", camera.GetViewMatrix());
            Shader.SetMatrix4("projection", camera.GetProjectionMatrix());
        }

        public void SetVertexAttribPointerAndArray(string attribute, int size, int stride, int offset)
        {
            int location = Shader.GetAttribLocation(attribute);
            GL.EnableVertexAttribArray(location);
            GL.VertexAttribPointer(location, size, VertexAttribPointerType.Float, false, stride, offset);
        }

        public void SendData()
        {
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferHandle);
                GL.BufferData(BufferTarget.ArrayBuffer, VertexesCount * sizeof(float), Vertexes, BufferUsageHint.DynamicDraw);

                GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferHandle);
                GL.BufferData(BufferTarget.ElementArrayBuffer, IndexesCount * sizeof(uint), Indexes, BufferUsageHint.DynamicDraw);
            }
        }

        public struct SVertexesAndIndexes
        {
            public float[] Vertexes;
            public uint[] Indexes;
        }
    }
}